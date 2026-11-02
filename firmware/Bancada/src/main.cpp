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
EmuladorVeiculoCP veiculoCP(PINO_CP_S7, PINO_CP_S8,
                            PINO_CP_S9_1, PINO_CP_S9_2,
                            PINO_CP_S10_C, PINO_CP_S10_D,
                            PINO_CP_S11_1, PINO_CP_S11_2,
                            PINO_CP_S12_1, PINO_CP_S12_2,
                            NIVEL_RELE_ATIVO, NIVEL_RELE_INATIVO,
                            TEMPO_BREAK_BEFORE_MAKE_CP_MS);
#endif

// ============================================================================
// 3. FUNÇÕES DE PROCESSAMENTO SERIAL (CLI)
// ============================================================================

void exibirHelp()
{
    Serial.println();
    Serial.println(F("=================================================="));
    Serial.println(F("            COMANDOS DA BANCADA (IEC 62196)       "));
    Serial.println(F("=================================================="));
    Serial.println(F(" cabo 0      -> Circuito Aberto (Desliga todas as chaves)"));
    Serial.println(F(" cabo 1      -> S1: Desconectado nominal (4700 Ohms)"));
    Serial.println(F(" cabo 2      -> S2: Cabo 13 A (1500 Ohms)"));
    Serial.println(F(" cabo 3      -> S3: Cabo 20 A (680 Ohms)"));
    Serial.println(F(" cabo 4      -> S4: Cabo 32 A (220 Ohms)"));
    Serial.println(F(" cabo 5      -> S5: Cabo 63 A (100 Ohms)"));
    Serial.println(F(" cabo 6      -> S6: Falha de Isolacao / Curto (47 Ohms)"));
    Serial.println(F(" cabo status -> Exibe relatorio do estado atual"));
    Serial.println(F("--------------------------------------------------"));
    Serial.println(F("        PILOTO DE CONTROLE - CP (IEC 61851-1)     "));
    Serial.println(F("--------------------------------------------------"));
#ifdef BANCADA_TEM_CP
    Serial.println(F(" estado ax    -> AX: Veiculo nao conectado (12 V)"));
    Serial.println(F(" estado bx+   -> BX limite superior (10 V)"));
    Serial.println(F(" estado bx    -> BX nominal (9 V)"));
    Serial.println(F(" estado bx-   -> BX limite inferior (8 V)"));
    Serial.println(F(" estado cx+   -> CX limite superior (7 V)"));
    Serial.println(F(" estado cx    -> CX nominal (6 V)"));
    Serial.println(F(" estado cx-   -> CX limite inferior (5 V)"));
    Serial.println(F(" estado dx+   -> DX limite superior (4 V)"));
    Serial.println(F(" estado dx    -> DX nominal (3 V)"));
    Serial.println(F(" estado dx-   -> DX limite inferior (2 V)"));
    Serial.println(F(" estado e     -> E: Falha / curto CP-PE (1 V)"));
    Serial.println(F(" estado a|b|c|d -> Atalhos para o nominal"));
    Serial.println(F(" estado status  -> Exibe relatorio do CP"));
    Serial.println(F(" Reles: S10.2 = ramo C (D26); S10.1 = ramo D (D27)"));
    Serial.println(F("        (S10.1 = S10.0 da Tabela II)"));
#else
    Serial.println(F(" estado ...   -> Indisponivel no Nano (use o Mega)"));
#endif
    Serial.println(F("--------------------------------------------------"));
    Serial.println(F(" debug stack -> Stack livre (high water mark) das tasks"));
    Serial.println(F(" help        -> Exibe este menu explicativo"));
    Serial.println(F("=================================================="));
}

// "debug stack": menor stack livre já observada (no AVR 1 word = 1 byte)
void imprimirStack()
{
    Serial.print(F("[STACK] SerialCLI livre: "));
    Serial.print((unsigned int)uxTaskGetStackHighWaterMark(xTarefaSerial));
    Serial.print(F(" de "));
    Serial.print((unsigned int)STACK_CABO);
    Serial.print(F(" | Blink livre: "));
    Serial.print((unsigned int)uxTaskGetStackHighWaterMark(xTarefaBlink));
    Serial.print(F(" de "));
    Serial.println((unsigned int)STACK_BLINK);
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
        Serial.println(F("OK"));
    }
    else
    {
        Serial.println(F("ERRO: Estado"));
    }
#else
    (void)arg;
    Serial.println(F("ERRO: CP indisponivel no Nano"));
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
            Serial.println(F("ERRO: Cabo"));
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
            Serial.println(F("OK"));
            return;
        }
        else
        {
            Serial.println(F("ERRO: Cabo"));
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
    Serial.println(F("ERRO"));
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
                        Serial.println(F("ERRO: Comando longo"));
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

    Serial.println();
    Serial.println(F("=================================================="));
    Serial.println(F("  PROJETO BANCADA - EMULADOR PP (IEC 62196) / CP  "));
    Serial.println(F("=================================================="));
    Serial.print(F(" Placa Detectada: "));
    Serial.println(F(NOME_PLACA));
    Serial.print(F(" Baud Rate:       "));
    Serial.println(SERIAL_BAUD_RATE);
    Serial.println(F(" Mutex Serial:    HABILITADO                      "));
    Serial.println(F(" Buffer Estatico: HABILITADO (32 bytes)           "));
    Serial.println(F(" Digite 'help' para ver os comandos disponiveis.  "));
    Serial.println(F("=================================================="));

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
        Serial.println(F("FALHA CRITICA: Impossivel alocar Mutex Serial!"));
    }
}

// ============================================================================
// 6. LOOP PRINCIPAL
// ============================================================================
void loop()
{
}
