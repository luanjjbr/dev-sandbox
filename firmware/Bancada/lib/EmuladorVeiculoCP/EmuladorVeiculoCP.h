/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: EmuladorVeiculoCP.h
 * Finalidade: Abstração do Emulador do Veículo no Piloto de Controle
 *             (Pino CP - IEC 61851-1 Anexo A), 10 relés D22..D31 no Mega
 *             (pinagem da placa EasyEDA: ver TabelaEstadosCP.h)
 * Microcontrolador: Arduino Mega 2560 (no Nano o CP não existe fisicamente)
 * ============================================================================
 */

#ifndef EMULADOR_VEICULO_CP_H
#define EMULADOR_VEICULO_CP_H

#include <Arduino.h>
#include <Arduino_FreeRTOS.h>
#include "TabelaEstadosCP.h"

class EmuladorVeiculoCP {
private:
    uint8_t _pinos[CP_TOTAL_RELES];  // Pinos na ordem do bitmask (K_PE .. K_D_SEL)
    uint8_t _nivelAtivo;             // Nível que fecha o relé
    uint8_t _nivelInativo;           // Nível que abre o relé
    uint16_t _tempoBreakMs;          // Pausa break-before-make (ms)
    CodigoEstadoCP _estadoAtual;

    void aplicarMascara(uint16_t mascara);
    TickType_t ticksDaPausa() const;  // pausa em ticks, garantindo >= _tempoBreakMs

public:
    /**
     * @brief Construtor: registra os 10 pinos dos relés físicos (ordem do bitmask)
     */
    EmuladorVeiculoCP(uint8_t pinoKPe, uint8_t pinoKFalha,
                      uint8_t pinoKBEn, uint8_t pinoKBSel,
                      uint8_t pinoKCdEn, uint8_t pinoKCdSel,
                      uint8_t pinoKCEn, uint8_t pinoKCSel,
                      uint8_t pinoKDEn, uint8_t pinoKDSel,
                      uint8_t nivelAtivo, uint8_t nivelInativo,
                      uint16_t tempoBreakMs);

    /**
     * @brief Boot seguro: pinos em OUTPUT e todos os relés abertos (Estado AX).
     *        Não usa o escalonador: pode ser chamado no setup() antes das tasks.
     */
    void begin();

    /**
     * @brief Comuta para o estado com break-before-make pela interseção:
     *        abre só os relés que saem, aguarda >= _tempoBreakMs (vTaskDelay,
     *        libera a CPU) e fecha só os que entram. Mesmo estado = no-op.
     *        Deve ser chamado de dentro de uma task FreeRTOS.
     */
    bool ativarEstado(CodigoEstadoCP estado);

    CodigoEstadoCP obterEstadoAtual() const { return _estadoAtual; }

    /**
     * @brief Imprime "[STATUS CP] ..." na Serial (o chamador detém o mutex)
     */
    void imprimirStatus() const;
};

#endif // EMULADOR_VEICULO_CP_H
