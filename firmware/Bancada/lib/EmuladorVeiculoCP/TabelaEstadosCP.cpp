/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: TabelaEstadosCP.cpp
 * Finalidade: Tabela estado -> relés e parser do comando "estado" (lógica pura)
 * ============================================================================
 */

#include "TabelaEstadosCP.h"

// Tabela canônica estado -> relés FÍSICOS energizados (índice = CodigoEstadoCP).
// Conferida contra o esquemático EasyEDA (placa real) e tabela2_estados_cp.csv.
// Ramo sem bypass (EN desligado) = limite superior; EN = nominal; EN+SEL =
// inferior. Os estados C/D mantêm o ramo B nominal (2740R) em paralelo, como a
// Tabela II (S9.1). C e D nunca ficam juntos: o K_CD_SEL é um SPDT.
//   Tabela II       ->  relés da placa
//   S7              ->  K_PE (D30)            S8    -> K_FALHA (D31)
//   S9.1 / S9.2     ->  K_B_EN / K_B_EN+K_B_SEL (D29 / D29+D28)
//   S10 ramo C / D  ->  K_CD_EN / K_CD_EN+K_CD_SEL (D27 / D27+D26)
//   S11.1 / S11.2   ->  K_C_EN / K_C_EN+K_C_SEL (D25 / D25+D24)
//   S12.1 / S12.2   ->  K_D_EN / K_D_EN+K_D_SEL (D23 / D23+D22)
static constexpr uint16_t CP_B_NOM = CP_K_PE | CP_K_B_EN;
static constexpr uint16_t CP_RAMO_C = CP_B_NOM | CP_K_CD_EN;
static constexpr uint16_t CP_RAMO_D = CP_B_NOM | CP_K_CD_EN | CP_K_CD_SEL;

static constexpr uint16_t TABELA_RELES_CP[CP_TOTAL_ESTADOS] = {
    /* AX_NOMINAL  */ CP_RELES_NENHUM,
    /* BX_SUPERIOR */ CP_K_PE,
    /* BX_NOMINAL  */ CP_B_NOM,
    /* BX_INFERIOR */ CP_B_NOM | CP_K_B_SEL,
    /* CX_SUPERIOR */ CP_RAMO_C,
    /* CX_NOMINAL  */ CP_RAMO_C | CP_K_C_EN,
    /* CX_INFERIOR */ CP_RAMO_C | CP_K_C_EN | CP_K_C_SEL,
    /* DX_SUPERIOR */ CP_RAMO_D,
    /* DX_NOMINAL  */ CP_RAMO_D | CP_K_D_EN,
    /* DX_INFERIOR */ CP_RAMO_D | CP_K_D_EN | CP_K_D_SEL,
    /* E_FALHA     */ CP_K_PE | CP_K_FALHA
};

// ============================================================================
// PROVA EM TEMPO DE COMPILAÇÃO (roda em todo "pio run")
// ============================================================================
// A troca de estado usa como intermediário a interseção (atual & nova): abre
// só os relés que saem, espera, e fecha só os que entram. As funções abaixo
// percorrem TODOS os estados e TODOS os pares e provam que:
//  (a) cada estado e cada interseção são combinações "coerentes" da árvore:
//      seletor só com o seu habilita; bypass de C só com a carga em C
//      (K_CD_EN ligado e K_CD_SEL desligado); bypass de D só com a carga em D;
//      K_CD_SEL só com K_CD_EN; K_FALHA só junto de K_PE;
//  (b) K_FALHA só aparece no Estado E;
//  (c) todo estado conectado (BX+ .. E, índices 1..10) tem K_PE, e a interseção
//      de dois deles também (o CP nunca "cai" para AX/12 V numa troca) e nunca
//      tem K_FALHA.
// Ex.: CX -> DX: a interseção mantém K_PE, K_B_EN e K_CD_EN e solta K_C_EN;
// com K_CD_SEL ainda desligado o ramo C fica sem bypass (CX+, ~7 V) durante a
// pausa, e só então K_CD_SEL comuta para D. Não é preciso abrir K_CD_EN.
static constexpr bool cpTem(uint16_t m, uint16_t b) { return (m & b) == b; }
static constexpr bool cpImplica(uint16_t m, uint16_t se, uint16_t entao)
{
    return !cpTem(m, se) || cpTem(m, entao);
}

static constexpr bool cpCombinacaoCoerente(uint16_t m)
{
    return cpImplica(m, CP_K_B_SEL, CP_K_B_EN) &&
           cpImplica(m, CP_K_C_SEL, CP_K_C_EN) &&
           cpImplica(m, CP_K_D_SEL, CP_K_D_EN) &&
           cpImplica(m, CP_K_CD_SEL, CP_K_CD_EN) &&
           (!cpTem(m, CP_K_C_EN) || (cpTem(m, CP_K_CD_EN) && !cpTem(m, CP_K_CD_SEL))) &&
           (!cpTem(m, CP_K_D_EN) || cpTem(m, CP_K_CD_EN | CP_K_CD_SEL)) &&
           cpImplica(m, CP_K_FALHA, CP_K_PE);
}

static constexpr uint8_t CP_INDICE_E = static_cast<uint8_t>(CodigoEstadoCP::E_FALHA);

static constexpr bool cpEstadoSeguro(uint8_t i)
{
    return cpCombinacaoCoerente(TABELA_RELES_CP[i]) &&
           (!cpTem(TABELA_RELES_CP[i], CP_K_FALHA) || (i == CP_INDICE_E)) &&
           ((i == 0U) || cpTem(TABELA_RELES_CP[i], CP_K_PE));
}

static constexpr bool cpTodosEstadosSeguros(uint8_t i)
{
    return (i >= CP_TOTAL_ESTADOS) ? true
         : (cpEstadoSeguro(i) && cpTodosEstadosSeguros(static_cast<uint8_t>(i + 1U)));
}

static constexpr bool cpIntersecaoSegura(uint8_t i, uint8_t j)
{
    return (i == j) ||
           (cpCombinacaoCoerente(TABELA_RELES_CP[i] & TABELA_RELES_CP[j]) &&
            cpTem(TABELA_RELES_CP[i] & TABELA_RELES_CP[j], CP_K_PE) &&
            !cpTem(TABELA_RELES_CP[i] & TABELA_RELES_CP[j], CP_K_FALHA));
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
              "estado incoerente com a arvore de reles, K_FALHA fora do E ou conectado sem K_PE");
static_assert(cpTodosParesConectadosSeguros(1U),
              "intersecao de dois estados conectados incoerente, sem K_PE ou com K_FALHA");

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
