/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: main.cpp
 * Finalidade: Interface Serial por Comandos (CLI) com FreeRTOS, EmuladorCabo
 *             (PP), EmuladorVeiculoCP (CP, só no Mega), Buffer Estático em C e
 *             Proteção Concorrente de UART via Mutex
 * Microcontroladores: Arduino Nano (ATmega328P) / Arduino Mega 2560
 * ============================================================================
 */

#include <Arduino.h>
#include <Arduino_FreeRTOS.h>
#include <task.h>
#include <semphr.h>
#include <string.h>
#include <ctype.h>
#include "ConfigHardware.h"
#include "SaidaSerial.h"   // TX por polling (ver lib/SaidaSerial/SaidaSerial.h)
#include "EmuladorCabo.h"

#if defined(PLACA_MEGA) || defined(__AVR_ATmega2560__)
    #define BANCADA_TEM_CP 1
    #include "EmuladorVeiculoCP.h"
#endif

// ============================================================================
// 1. RECURSOS DE SINCRONIZAÇÃO FREERTOS
// ============================================================================
SemaphoreHandle_t xSerialMutex = NULL;
TaskHandle_t xTarefaBlink  = NULL;   // Usados só no "debug stack"
TaskHandle_t xTarefaSerial = NULL;

// ============================================================================
// 2. INSTANCIAÇÃO DOS OBJETOS DA BANCADA (POO)
// ============================================================================
EmuladorCabo cabo(PINO_CABO_S1, PINO_CABO_S2, PINO_CABO_S3,
                  PINO_CABO_S4, PINO_CABO_S5, PINO_CABO_S6);

#ifdef BANCADA_TEM_CP
EmuladorVeiculoCP veiculoCP(PINO_CP_K_PE, PINO_CP_K_FALHA,
                            PINO_CP_K_B_EN, PINO_CP_K_B_SEL,
                            PINO_CP_K_CD_EN, PINO_CP_K_CD_SEL,
                            PINO_CP_K_C_EN, PINO_CP_K_C_SEL,
                            PINO_CP_K_D_EN, PINO_CP_K_D_SEL,
                            NIVEL_RELE_ATIVO, NIVEL_RELE_INATIVO,
                            TEMPO_BREAK_BEFORE_MAKE_CP_MS);
#endif

// ============================================================================
// 3. FUNÇÕES DE PROCESSAMENTO SERIAL (CLI)
// ============================================================================

void exibirHelp()
{
    Saida.println();
    Saida.println(F("=================================================="));
    Saida.println(F("            COMANDOS DA BANCADA (IEC 62196)       "));
    Saida.println(F("=================================================="));
    Saida.println(F(" cabo 0      -> Circuito Aberto (Desliga todas as chaves)"));
    Saida.println(F(" cabo 1      -> S1: Desconectado nominal (4700 Ohms)"));
    Saida.println(F(" cabo 2      -> S2: Cabo 13 A (1500 Ohms)"));
    Saida.println(F(" cabo 3      -> S3: Cabo 20 A (680 Ohms)"));
    Saida.println(F(" cabo 4      -> S4: Cabo 32 A (220 Ohms)"));
    Saida.println(F(" cabo 5      -> S5: Cabo 63 A (100 Ohms)"));
    Saida.println(F(" cabo 6      -> S6: Falha de Isolacao / Curto (47 Ohms)"));
    Saida.println(F(" cabo status -> Exibe relatorio do estado atual"));
    Saida.println(F("--------------------------------------------------"));
    Saida.println(F("        PILOTO DE CONTROLE - CP (IEC 61851-1)     "));
    Saida.println(F("--------------------------------------------------"));
#ifdef BANCADA_TEM_CP
    Saida.println(F(" estado ax    -> AX: Veiculo nao conectado (12 V)"));
    Saida.println(F(" estado bx+   -> BX limite superior (10 V)"));
    Saida.println(F(" estado bx    -> BX nominal (9 V)"));
    Saida.println(F(" estado bx-   -> BX limite inferior (8 V)"));
    Saida.println(F(" estado cx+   -> CX limite superior (7 V)"));
    Saida.println(F(" estado cx    -> CX nominal (6 V)"));
    Saida.println(F(" estado cx-   -> CX limite inferior (5 V)"));
    Saida.println(F(" estado dx+   -> DX limite superior (4 V)"));
    Saida.println(F(" estado dx    -> DX nominal (3 V)"));
    Saida.println(F(" estado dx-   -> DX limite inferior (2 V)"));
    Saida.println(F(" estado e     -> E: Falha / curto CP-PE (1 V)"));
    Saida.println(F(" estado a|b|c|d -> Atalhos para o nominal"));
    Saida.println(F(" estado status  -> Exibe relatorio do CP"));
    Saida.println(F(" Reles (placa EasyEDA): D30 = PE (S7); D31 = falha (S8)"));
    Saida.println(F("        D29/D28 ramo B; D27 carga, D26 C/D; D25/D24 C; D23/D22 D"));
#else
    Saida.println(F(" estado ...   -> Indisponivel no Nano (use o Mega)"));
#endif
    Saida.println(F("--------------------------------------------------"));
    Saida.println(F(" debug stack -> Stack livre (high water mark) das tasks"));
    Saida.println(F(" help        -> Exibe este menu explicativo"));
    Saida.println(F("=================================================="));
}

// "debug stack": menor stack livre já observada (no AVR 1 word = 1 byte).
// ATENÇÃO: no port AVR, uxTaskGetStackHighWaterMark devolve UBaseType_t de
// 8 bits (máx. 255). Se algum stack passar de 255 words, troque por
// uxTaskGetStackHighWaterMark2 (configSTACK_DEPTH_TYPE) para não truncar; ela
// exige INCLUDE_uxTaskGetStackHighWaterMark2 = 1, que o FreeRTOSConfig.h do
// feilipu não habilita. Hoje: SerialCLI 256 (Mega) / 128 (Nano); o valor
// livre real fica abaixo de 255 porque a task sempre usa parte do stack.
void imprimirStack()
{
    Saida.print(F("[STACK] SerialCLI livre: "));
    Saida.print((unsigned int)uxTaskGetStackHighWaterMark(xTarefaSerial));
    Saida.print(F(" de "));
    Saida.print((unsigned int)STACK_CABO);
    Saida.print(F(" | Blink livre: "));
    Saida.print((unsigned int)uxTaskGetStackHighWaterMark(xTarefaBlink));
    Saida.print(F(" de "));
    Saida.println((unsigned int)STACK_BLINK);
}

// true se "comando" começa pela palavra inteira "palavra" (fim ou espaço depois)
static bool comecaComPalavra(const char *comando, const char *palavra)
{
    const size_t n = strlen(palavra);
    return strncmp(comando, palavra, n) == 0 &&
           (comando[n] == '\0' || isspace((unsigned char)comando[n]));
}

// Trata o argumento de "estado ..." (já em minúsculas, sem espaços finais)
void processarComandoEstado(char *arg)
{
#ifdef BANCADA_TEM_CP
    while (isspace((unsigned char)*arg)) {
        arg++;
    }

    if (strcmp(arg, "status") == 0)
    {
        veiculoCP.imprimirStatus();
        return;
    }

    // Obs.: ativarEstado() faz vTaskDelay (>= TEMPO_BREAK_BEFORE_MAKE_CP_MS)
    // com o xSerialMutex tomado pelo chamador; ver EmuladorVeiculoCP.cpp.
    CodigoEstadoCP estado;
    if (cpInterpretarEstado(arg, &estado) && veiculoCP.ativarEstado(estado))
    {
        Saida.println(F("OK"));
    }
    else
    {
        Saida.println(F("ERRO: Estado"));
    }
#else
    (void)arg;
    Saida.println(F("ERRO: CP indisponivel no Nano"));
#endif
}

void processarComando(char *comando)
{
    // Remove espaços iniciais
    while (isspace((unsigned char)*comando)) {
        comando++;
    }

    // Remove espaços finais
    int len = strlen(comando);
    while (len > 0 && isspace((unsigned char)comando[len - 1])) {
        comando[--len] = '\0';
    }

    if (len == 0) {
        return;
    }

    // Converte para minúsculas para comparação uniforme
    for (int i = 0; i < len; i++) {
        comando[i] = tolower((unsigned char)comando[i]);
    }

    // 1. Comando de Ajuda
    if (strcmp(comando, "help") == 0 || strcmp(comando, "?") == 0)
    {
        exibirHelp();
        return;
    }

    // 2. Comandos do tipo "cabo ..." (palavra inteira: "cabo1" -> ERRO)
    if (comecaComPalavra(comando, "cabo"))
    {
        char *arg = comando + 4;
        while (isspace((unsigned char)*arg)) {
            arg++;
        }

        // Se digitou apenas "cabo" sem argumento
        if (*arg == '\0')
        {
            Saida.println(F("ERRO: Cabo"));
            return;
        }

        // Subcomando de status
        if (strcmp(arg, "status") == 0)
        {
            cabo.imprimirStatus();
            return;
        }

        // Subcomando numérico "cabo 0" a "cabo 6"
        if (arg[0] >= '0' && arg[0] <= '6' && arg[1] == '\0')
        {
            int canal = arg[0] - '0';
            cabo.ativarCanal(canal);
            Saida.println(F("OK"));
            return;
        }
        else
        {
            Saida.println(F("ERRO: Cabo"));
            return;
        }
    }

    // 3. Comandos do tipo "estado ..." (Piloto de Controle - CP)
    if (comecaComPalavra(comando, "estado"))
    {
        processarComandoEstado(comando + 6);
        return;
    }

    // 4. Diagnóstico de stack das tasks FreeRTOS
    if (strcmp(comando, "debug stack") == 0)
    {
        imprimirStack();
        return;
    }

    // 5. Qualquer outro comando desconhecido
    Saida.println(F("ERRO"));
}

// ============================================================================
// 4. TAREFAS FREERTOS
// ============================================================================

void tarefaBlink(void *pvParameters)
{
    (void)pvParameters;
    pinMode(PINO_LED_STATUS, OUTPUT);
    bool estadoLed = LOW;

    for (;;)
    {
        estadoLed = !estadoLed;
        digitalWrite(PINO_LED_STATUS, estadoLed);
        vTaskDelay(pdMS_TO_TICKS(500));
    }
}

void tarefaSerial(void *pvParameters)
{
    (void)pvParameters;

    // Buffer estático em C (Zero fragmentação de heap)
    char bufferEntrada[32];
    uint8_t indiceBuffer = 0;
    bool linhaLonga = false;   // linha passou de 31 caracteres: descartar inteira

    for (;;)
    {
        while (Serial.available() > 0)
        {
            char c = (char)Serial.read();

            if (c == '\r')
            {
                continue; // Ignora retorno de carro
            }

            if (c == '\n')
            {
                bufferEntrada[indiceBuffer] = '\0';

                if (xSerialMutex != NULL && xSemaphoreTake(xSerialMutex, portMAX_DELAY) == pdTRUE)
                {
                    if (linhaLonga)
                    {
                        // Nunca executa um comando truncado (ex.: prefixo válido)
                        Saida.println(F("ERRO: Comando longo"));
                    }
                    else
                    {
                        processarComando(bufferEntrada);
                    }
                    xSemaphoreGive(xSerialMutex);
                }

                indiceBuffer = 0; // Reinicia o buffer
                linhaLonga = false;
            }
            else
            {
                // Proteção contra estouro de buffer (Buffer Overflow)
                if (indiceBuffer < (sizeof(bufferEntrada) - 1))
                {
                    bufferEntrada[indiceBuffer++] = c;
                }
                else
                {
                    linhaLonga = true;
                }
            }
        }

        vTaskDelay(pdMS_TO_TICKS(20));
    }
}

// ============================================================================
// 5. INICIALIZAÇÃO DO SISTEMA (SETUP)
// ============================================================================
void setup()
{
    // Boot seguro: todas as saídas de chave/relé em nível inativo ANTES das
    // tasks (recomendado também pull-down de 10k na base/gate dos drivers,
    // que segura o corte durante o reset, antes de o firmware rodar).
#ifdef BANCADA_TEM_CP
    veiculoCP.begin();   // CP: relés abertos (Estado AX)
#endif
    cabo.begin();        // PP: chaves S1..S6 em LOW (circuito aberto)

    Serial.begin(SERIAL_BAUD_RATE);

    xSerialMutex = xSemaphoreCreateMutex();

    Saida.println();
    Saida.println(F("=================================================="));
    Saida.println(F("  PROJETO BANCADA - EMULADOR PP (IEC 62196) / CP  "));
    Saida.println(F("=================================================="));
    Saida.print(F(" Placa Detectada: "));
    Saida.println(F(NOME_PLACA));
    Saida.print(F(" Baud Rate:       "));
    Saida.println(SERIAL_BAUD_RATE);
    Saida.println(F(" Mutex Serial:    HABILITADO                      "));
    Saida.println(F(" Buffer Estatico: HABILITADO (32 bytes)           "));
    Saida.println(F(" Digite 'help' para ver os comandos disponiveis.  "));
    Saida.println(F("=================================================="));

    if (xSerialMutex != NULL)
    {
        xTaskCreate(
            tarefaBlink,
            "Blink",
            STACK_BLINK,
            NULL,
            1,
            &xTarefaBlink
        );

        xTaskCreate(
            tarefaSerial,
            "SerialCLI",
            STACK_CABO,
            NULL,
            1,
            &xTarefaSerial
        );
    }
    else
    {
        Saida.println(F("FALHA CRITICA: Impossivel alocar Mutex Serial!"));
    }
}

// ============================================================================
// 6. LOOP PRINCIPAL
// ============================================================================
void loop()
{
}
