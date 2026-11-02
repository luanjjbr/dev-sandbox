/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: ConfigHardware.h
 * Finalidade: Mapeamento de pinos e configurações de hardware
 * Microcontroladores suportados: Arduino Nano (ATmega328P) e Arduino Mega 2560
 * ============================================================================
 */

#ifndef CONFIG_HARDWARE_H
#define CONFIG_HARDWARE_H

#include <Arduino.h>

// ============================================================================
// 1. CONFIGURAÇÕES DE COMUNICAÇÃO SERIAL
// ============================================================================
#ifndef SERIAL_BAUD_RATE
#define SERIAL_BAUD_RATE 9600
#endif

// ============================================================================
// 2. SELEÇÃO DE PINAGEM E IDENTIFICAÇÃO DE PLACA
// ============================================================================
#if defined(PLACA_MEGA) || defined(__AVR_ATmega2560__)
    #define NOME_PLACA "Arduino Mega 2560 (5V)"
    #define PINO_LED_STATUS 13
#elif defined(PLACA_NANO) || defined(__AVR_ATmega328P__)
    #define NOME_PLACA "Arduino Nano (5V)"
    #define PINO_LED_STATUS 13
#else
    #define NOME_PLACA "Arduino AVR Generico (5V)"
    #define PINO_LED_STATUS 13
#endif

// Tamanho padrão de stack para tarefas em microcontroladores AVR (em words;
// no port AVR StackType_t = uint8_t, logo words = bytes)
#define STACK_BLINK 128
#if defined(PLACA_MEGA) || defined(__AVR_ATmega2560__)
    // Mega (8 KB SRAM): folga extra para o parser + status do CP na SerialCLI
    #define STACK_CABO  256
#else
    #define STACK_CABO  128
#endif

// ============================================================================
// 3. PINOS DO EMULADOR DE CABO - PINO PP (IEC 62196-2)
// ============================================================================
// Acionamento das chaves MOSFET S1 a S6 do barramento de resistores para PE
#define PINO_CABO_S1    4    // S1: 4700 Ohms (Desconectado nominal)
#define PINO_CABO_S2    5    // S2: 1500 Ohms (Cabo 13 A)
#define PINO_CABO_S3    6    // S3:  680 Ohms (Cabo 20 A)
#define PINO_CABO_S4    7    // S4:  220 Ohms (Cabo 32 A)
#define PINO_CABO_S5    8    // S5:  100 Ohms (Cabo 63 A / 70 A)
#define PINO_CABO_S6    9    // S6:   47 Ohms (Simulação de Falha / Curto)

// ============================================================================
// 4. LÓGICA DE ACIONAMENTO DOS RELÉS (5V)
// ============================================================================
// Nível lógico para o driver de relé (HIGH para NPN S8050 / LOW para optoacoplador invertido)
#define NIVEL_RELE_ATIVO    HIGH
#define NIVEL_RELE_INATIVO  LOW

// ============================================================================
// 5. PINOS DO EMULADOR DE VEÍCULO - PINO CP (IEC 61851-1 ANEXO A)
// ============================================================================
// Acionamento dos 10 relés do CP conforme a PLACA REAL (esquemático EasyEDA
// bancada_de_teste_reles_corrigido, 2026-10-09; decisão do usuário 2026-11-02).
// Driver: Dxx -> 1k -> S8050 -> bobina SRD-05VDC (ativo em HIGH).
// Cada ramo é uma árvore "habilita (EN) + seletor (SEL)": o SEL só atua com o
// seu EN ligado; NC do SEL = tap nominal, NO = tap inferior. Ver
// lib/EmuladorVeiculoCP/TabelaEstadosCP.h e docs/pinout.md.
#if defined(PLACA_MEGA) || defined(__AVR_ATmega2560__)
    #define PINO_CP_K_PE      30   // RLY6  (S7): C-NO liga o retorno ao PE (aberto = AX, 12 V)
    #define PINO_CP_K_FALHA   31   // RLY1  (S8): C-NO liga o CP (antes do diodo) ao R29 120R (Estado E)
    #define PINO_CP_K_B_EN    29   // RLY7 : habilita o bypass do ramo B (S9.1 -> 2740R, BX 9 V)
    #define PINO_CP_K_B_SEL   28   // RLY2 : seletor B, NO = 1820R (S9.2, BX- 8 V; exige K_B_EN)
    #define PINO_CP_K_CD_EN   27   // RLY5 : habilita a carga (ramo C ou D em paralelo com o B)
    #define PINO_CP_K_CD_SEL  26   // RLY8 : seletor SPDT, NC = ramo C / NO = ramo D (exige K_CD_EN)
    #define PINO_CP_K_C_EN    25   // RLY3 : habilita o bypass do ramo C (S11.1 -> 1301R, CX 6 V)
    #define PINO_CP_K_C_SEL   24   // RLY4 : seletor C, NO = 909R (S11.2, CX- 5 V; exige K_C_EN)
    #define PINO_CP_K_D_EN    23   // RLY9 : habilita o bypass do ramo D (S12.1 -> 270R, DX 3 V)
    #define PINO_CP_K_D_SEL   22   // RLY10: seletor D, NO = 140R (S12.2, DX- 2 V; exige K_D_EN)
#endif

// Break-before-make do CP: pausa mínima (ms) entre abrir os relés que saem e
// fechar os que entram (acomodação mecânica dos contatos). Definido pelo
// usuário em 2026-11-02. Com o tick de ~16 ms do FreeRTOS no AVR, a pausa real
// fica em 32..48 ms (ver EmuladorVeiculoCP::ticksDaPausa).
#define TEMPO_BREAK_BEFORE_MAKE_CP_MS  20

#endif // CONFIG_HARDWARE_H

