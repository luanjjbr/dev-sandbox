/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: TabelaEstadosCP.cpp
 * Finalidade: Tabela estado -> relés e parser do comando "estado" (lógica pura)
 * ============================================================================
 */

#include "TabelaEstadosCP.h"

// Tabela canônica estado -> relés fechados (índice = CodigoEstadoCP).
// Conferida contra tabela_estados_cp.csv, tabela2_estados_cp.csv e
// LTSpice/teste3.asc. Ramos B/C/D sem bypass acionado = limite superior
// (S9.0/S11.0/S12.0 da tabela2 não são relés físicos).
static constexpr uint16_t TABELA_RELES_CP[CP_TOTAL_ESTADOS] = {
    /* AX_NOMINAL  */ CP_RELES_NENHUM,
    /* BX_SUPERIOR */ CP_RELE_S7,
    /* BX_NOMINAL  */ CP_RELE_S7 | CP_RELE_S9_1,
    /* BX_INFERIOR */ CP_RELE_S7 | CP_RELE_S9_2,
    /* CX_SUPERIOR */ CP_RELE_S7 | CP_RELE_S9_1 | CP_RELE_S10_C,
    /* CX_NOMINAL  */ CP_RELE_S7 | CP_RELE_S9_1 | CP_RELE_S10_C | CP_RELE_S11_1,
    /* CX_INFERIOR */ CP_RELE_S7 | CP_RELE_S9_1 | CP_RELE_S10_C | CP_RELE_S11_2,
    /* DX_SUPERIOR */ CP_RELE_S7 | CP_RELE_S9_1 | CP_RELE_S10_D,
    /* DX_NOMINAL  */ CP_RELE_S7 | CP_RELE_S9_1 | CP_RELE_S10_D | CP_RELE_S12_1,
    /* DX_INFERIOR */ CP_RELE_S7 | CP_RELE_S9_1 | CP_RELE_S10_D | CP_RELE_S12_2,
    /* E_FALHA     */ CP_RELE_S7 | CP_RELE_S8
};

// ============================================================================
// PROVA EM TEMPO DE COMPILAÇÃO (roda em todo "pio run")
// ============================================================================
// A troca de estado usa como intermediário a interseção (atual & nova): abre
// só os relés que saem, espera, e fecha só os que entram. As funções abaixo
// percorrem TODOS os pares de estados e provam que:
//  (a) cada estado: ramos C e D nunca juntos; S8 só no Estado E;
//  (b) para dois estados conectados distintos (BX+ .. E, índices 1..10), a
//      interseção contém S7 (o CP nunca "cai" para AX/12 V numa troca entre
//      estados conectados), nunca contém S8 e nunca tem C+D juntos.
// Como a interseção é subconjunto dos dois estados, nenhum resistor entra em
// paralelo que não exista no estado de origem ou de destino.
static constexpr uint16_t CP_RAMOS_C_D = CP_RELE_S10_C | CP_RELE_S10_D;
static constexpr uint8_t  CP_INDICE_E  = static_cast<uint8_t>(CodigoEstadoCP::E_FALHA);

static constexpr bool cpEstadoSeguro(uint8_t i)
{
    return ((TABELA_RELES_CP[i] & CP_RAMOS_C_D) != CP_RAMOS_C_D) &&
           (((TABELA_RELES_CP[i] & CP_RELE_S8) == 0U) || (i == CP_INDICE_E));
}

static constexpr bool cpTodosEstadosSeguros(uint8_t i)
{
    return (i >= CP_TOTAL_ESTADOS) ? true
         : (cpEstadoSeguro(i) && cpTodosEstadosSeguros(static_cast<uint8_t>(i + 1U)));
}

static constexpr bool cpIntersecaoSegura(uint8_t i, uint8_t j)
{
    return (i == j) ||
           ((((TABELA_RELES_CP[i] & TABELA_RELES_CP[j]) & CP_RELE_S7) == CP_RELE_S7) &&
            (((TABELA_RELES_CP[i] & TABELA_RELES_CP[j]) & CP_RELE_S8) == 0U) &&
            (((TABELA_RELES_CP[i] & TABELA_RELES_CP[j]) & CP_RAMOS_C_D) != CP_RAMOS_C_D));
}

static constexpr bool cpLinhaSegura(uint8_t i, uint8_t j)
{
    return (j >= CP_TOTAL_ESTADOS) ? true
         : (cpIntersecaoSegura(i, j) && cpLinhaSegura(i, static_cast<uint8_t>(j + 1U)));
}

static constexpr bool cpTodosParesConectadosSeguros(uint8_t i)
{
    return (i >= CP_TOTAL_ESTADOS) ? true
         : (cpLinhaSegura(i, 1U) && cpTodosParesConectadosSeguros(static_cast<uint8_t>(i + 1U)));
}

static_assert(CP_TOTAL_RELES <= 16, "bitmask de reles cabe em uint16_t");
static_assert(sizeof(TABELA_RELES_CP) / sizeof(TABELA_RELES_CP[0]) == CP_TOTAL_ESTADOS,
              "uma linha por estado");
static_assert(TABELA_RELES_CP[0] == CP_RELES_NENHUM, "AX = todos os reles abertos");
static_assert(cpTodosEstadosSeguros(0U),
              "estado com ramos C+D juntos ou S8 fora do Estado E");
static_assert(cpTodosParesConectadosSeguros(1U),
              "intersecao de dois estados conectados sem S7, com S8 ou com C+D");

// ============================================================================

uint16_t cpRelesDoEstado(CodigoEstadoCP estado)
{
    const uint8_t indice = static_cast<uint8_t>(estado);
    if (indice >= CP_TOTAL_ESTADOS) {
        return CP_RELES_NENHUM;
    }
    return TABELA_RELES_CP[indice];
}

bool cpInterpretarEstado(const char *arg, CodigoEstadoCP *saida)
{
    if (arg == nullptr || saida == nullptr || arg[0] == '\0') {
        return false;
    }

    const char letra = arg[0];

    // Estado E: "e"
    if (letra == 'e') {
        if (arg[1] != '\0') {
            return false;
        }
        *saida = CodigoEstadoCP::E_FALHA;
        return true;
    }

    // AX: "a" ou "ax"
    if (letra == 'a') {
        if (arg[1] == '\0' || (arg[1] == 'x' && arg[2] == '\0')) {
            *saida = CodigoEstadoCP::AX_NOMINAL;
            return true;
        }
        return false;
    }

    // BX / CX / DX: base do grupo = estado SUPERIOR; +1 nominal; +2 inferior
    uint8_t base;
    switch (letra) {
        case 'b': base = static_cast<uint8_t>(CodigoEstadoCP::BX_SUPERIOR); break;
        case 'c': base = static_cast<uint8_t>(CodigoEstadoCP::CX_SUPERIOR); break;
        case 'd': base = static_cast<uint8_t>(CodigoEstadoCP::DX_SUPERIOR); break;
        default:  return false;
    }

    uint8_t deslocamento;
    if (arg[1] == '\0') {
        deslocamento = 1;                       // alias "b|c|d" = nominal
    } else if (arg[1] == 'x') {
        switch (arg[2]) {
            case '\0': deslocamento = 1; break; // "bx"  = nominal
            case '+':  deslocamento = 0; break; // "bx+" = limite superior
            case '-':  deslocamento = 2; break; // "bx-" = limite inferior
            default:   return false;
        }
        if (arg[2] != '\0' && arg[3] != '\0') {
            return false;
        }
    } else {
        return false;
    }

    *saida = static_cast<CodigoEstadoCP>(base + deslocamento);
    return true;
}
