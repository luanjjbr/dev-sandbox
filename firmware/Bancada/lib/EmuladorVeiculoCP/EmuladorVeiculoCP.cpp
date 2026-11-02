/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: EmuladorVeiculoCP.cpp
 * Finalidade: Implementação do EmuladorVeiculoCP com Break-Before-Make pela
 *             interseção (abre só os relés que saem, espera, fecha os novos)
 * Microcontrolador: Arduino Mega 2560
 * ============================================================================
 */

#include "EmuladorVeiculoCP.h"
#include <Arduino_FreeRTOS.h>
#include <task.h>

EmuladorVeiculoCP::EmuladorVeiculoCP(uint8_t pinoKPe, uint8_t pinoKFalha,
                                     uint8_t pinoKBEn, uint8_t pinoKBSel,
                                     uint8_t pinoKCdEn, uint8_t pinoKCdSel,
                                     uint8_t pinoKCEn, uint8_t pinoKCSel,
                                     uint8_t pinoKDEn, uint8_t pinoKDSel,
                                     uint8_t nivelAtivo, uint8_t nivelInativo,
                                     uint16_t tempoBreakMs)
    : _nivelAtivo(nivelAtivo),
      _nivelInativo(nivelInativo),
      _tempoBreakMs(tempoBreakMs),
      _estadoAtual(CodigoEstadoCP::AX_NOMINAL)
{
    _pinos[0] = pinoKPe;
    _pinos[1] = pinoKFalha;
    _pinos[2] = pinoKBEn;
    _pinos[3] = pinoKBSel;
    _pinos[4] = pinoKCdEn;
    _pinos[5] = pinoKCdSel;
    _pinos[6] = pinoKCEn;
    _pinos[7] = pinoKCSel;
    _pinos[8] = pinoKDEn;
    _pinos[9] = pinoKDSel;
}

void EmuladorVeiculoCP::aplicarMascara(uint16_t mascara)
{
    for (uint8_t i = 0; i < CP_TOTAL_RELES; i++)
    {
        const bool fechado = (mascara & (1U << i)) != 0U;
        digitalWrite(_pinos[i], fechado ? _nivelAtivo : _nivelInativo);
    }
}

void EmuladorVeiculoCP::begin()
{
    // Nível inativo escrito ANTES de virar OUTPUT: evita pulso no relé quando
    // NIVEL_RELE_ATIVO = LOW (módulos optoacoplados ativos em nível baixo).
    for (uint8_t i = 0; i < CP_TOTAL_RELES; i++)
    {
        digitalWrite(_pinos[i], _nivelInativo);
        pinMode(_pinos[i], OUTPUT);
    }
    _estadoAtual = CodigoEstadoCP::AX_NOMINAL;
}

TickType_t EmuladorVeiculoCP::ticksDaPausa() const
{
    // O tick do FreeRTOS no AVR (feilipu, WDT 15 ms) é de ~16 ms: pdMS_TO_TICKS(20)
    // daria 1 tick, e vTaskDelay(1) pode retornar quase imediatamente (espera só
    // até o PRÓXIMO tick). Para GARANTIR no mínimo _tempoBreakMs: arredonda para
    // cima em ticks e soma 1 tick (20 ms -> 3 ticks = 32..48 ms reais).
    return static_cast<TickType_t>(
        ((_tempoBreakMs + portTICK_PERIOD_MS - 1U) / portTICK_PERIOD_MS) + 1U);
}

bool EmuladorVeiculoCP::ativarEstado(CodigoEstadoCP estado)
{
    if (static_cast<uint8_t>(estado) >= CP_TOTAL_ESTADOS)
    {
        return false;
    }

    // Mesmo estado: nada a comutar (no-op, sem tocar nos relés)
    if (estado == _estadoAtual)
    {
        return true;
    }

    const uint16_t mascaraAtual = cpRelesDoEstado(_estadoAtual);
    const uint16_t mascaraNova  = cpRelesDoEstado(estado);
    // Intermediário = interseção. Provado em TabelaEstadosCP.cpp (static_assert):
    // entre estados conectados ela mantém K_PE, nunca tem K_FALHA e é sempre uma
    // combinação coerente da árvore de relés (seletor só com o seu habilita);
    // por ser subconjunto dos dois estados, não cria paralelo espúrio.
    const uint16_t mascaraIntermediaria = mascaraAtual & mascaraNova;

    const bool algumReleSai    = (mascaraIntermediaria != mascaraAtual);
    const bool algumReleEntra  = (mascaraNova != mascaraIntermediaria);

    // 1. BREAK: abre só os relés que saem
    if (algumReleSai)
    {
        aplicarMascara(mascaraIntermediaria);
    }

    // 2. Acomodação mecânica: só quando algum relé SAIU (há contato abrindo) e
    // algum vai ENTRAR (há o que proteger). Só fechar relés (ex.: AX -> BX,
    // BX+ -> BX) ou só abrir (ex.: CX -> CX+, -> AX) não exige pausa.
    // ATENÇÃO: o chamador (processarComando) detém o xSerialMutex; esta pausa
    // roda DENTRO do mutex (até ~48 ms). Só a SerialCLI usa a Serial, e a
    // tarefaBlink não depende do mutex, então nenhuma task fica bloqueada.
    if (algumReleSai && algumReleEntra && _tempoBreakMs > 0U)
    {
        vTaskDelay(ticksDaPausa());
    }

    // 3. MAKE: fecha só os relés que entram (idempotente se nada entra)
    aplicarMascara(mascaraNova);

    _estadoAtual = estado;
    return true;
}

void EmuladorVeiculoCP::imprimirStatus() const
{
    Serial.print(F("[STATUS CP] Estado: "));
    switch (_estadoAtual)
    {
        case CodigoEstadoCP::AX_NOMINAL:  Serial.print(F("AX | Vcp: 12 V | Veiculo nao conectado")); break;
        case CodigoEstadoCP::BX_SUPERIOR: Serial.print(F("BX+ | Vcp: 10 V | Conectado, limite superior")); break;
        case CodigoEstadoCP::BX_NOMINAL:  Serial.print(F("BX | Vcp: 9 V | Conectado, nominal")); break;
        case CodigoEstadoCP::BX_INFERIOR: Serial.print(F("BX- | Vcp: 8 V | Conectado, limite inferior")); break;
        case CodigoEstadoCP::CX_SUPERIOR: Serial.print(F("CX+ | Vcp: 7 V | Carga sem ventilacao, limite superior")); break;
        case CodigoEstadoCP::CX_NOMINAL:  Serial.print(F("CX | Vcp: 6 V | Carga sem ventilacao, nominal")); break;
        case CodigoEstadoCP::CX_INFERIOR: Serial.print(F("CX- | Vcp: 5 V | Carga sem ventilacao, limite inferior")); break;
        case CodigoEstadoCP::DX_SUPERIOR: Serial.print(F("DX+ | Vcp: 4 V | Carga com ventilacao, limite superior")); break;
        case CodigoEstadoCP::DX_NOMINAL:  Serial.print(F("DX | Vcp: 3 V | Carga com ventilacao, nominal")); break;
        case CodigoEstadoCP::DX_INFERIOR: Serial.print(F("DX- | Vcp: 2 V | Carga com ventilacao, limite inferior")); break;
        case CodigoEstadoCP::E_FALHA:     Serial.print(F("E | Vcp: 1 V | Falha / curto CP-PE")); break;
        default:                          Serial.print(F("Desconhecido")); break;
    }

    // Relés listados pelo PINO físico que os aciona (placa EasyEDA), ex.:
    // "Reles: D30 D29" no BX. O app só lê o campo "Estado".
    Serial.print(F(" | Reles: "));
    const uint16_t mascara = cpRelesDoEstado(_estadoAtual);
    if (mascara == CP_RELES_NENHUM)
    {
        Serial.print(F("nenhum"));
    }
    bool primeiro = true;
    for (uint8_t i = 0; i < CP_TOTAL_RELES; i++)
    {
        if ((mascara & (1U << i)) != 0U)
        {
            if (!primeiro)
            {
                Serial.print(' ');
            }
            Serial.print('D');
            Serial.print(_pinos[i]);
            primeiro = false;
        }
    }
    Serial.println();
}
