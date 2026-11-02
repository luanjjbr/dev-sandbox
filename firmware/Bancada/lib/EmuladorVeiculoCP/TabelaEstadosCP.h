/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: TabelaEstadosCP.h
 * Finalidade: Lógica pura do Piloto de Controle (CP - IEC 61851-1 Anexo A):
 *             enum dos 11 estados emulados pelo veículo, tabela estado ->
 *             relés (bitmask) e parser do argumento do comando "estado".
 *             Sem dependência do Arduino (verificável em tempo de compilação).
 * Fonte da tabela: tabela_estados_cp.csv / tabela2_estados_cp.csv /
 *                  LTSpice/teste3.asc / docs/pinout.md
 * ============================================================================
 */

#ifndef TABELA_ESTADOS_CP_H
#define TABELA_ESTADOS_CP_H

#include <stdint.h>

/**
 * @brief Os 11 estados que o veículo emula no CP (Tabela II da IEC 61851-1).
 */
enum class CodigoEstadoCP : uint8_t {
    AX_NOMINAL = 0,  // 12 V - Veículo não conectado (S7 aberto)
    BX_SUPERIOR,     // 10 V - Conectado (standby), limite superior
    BX_NOMINAL,      //  9 V - Conectado (standby), nominal
    BX_INFERIOR,     //  8 V - Conectado (standby), limite inferior
    CX_SUPERIOR,     //  7 V - Carga sem ventilação, limite superior
    CX_NOMINAL,      //  6 V - Carga sem ventilação, nominal
    CX_INFERIOR,     //  5 V - Carga sem ventilação, limite inferior
    DX_SUPERIOR,     //  4 V - Carga com ventilação, limite superior
    DX_NOMINAL,      //  3 V - Carga com ventilação, nominal
    DX_INFERIOR,     //  2 V - Carga com ventilação, limite inferior
    E_FALHA          //  1 V - Curto CP-PE antes do diodo (S8)
};

static const uint8_t CP_TOTAL_ESTADOS = 11;
static const uint8_t CP_TOTAL_RELES   = 10;

// ----------------------------------------------------------------------------
// Bit de cada relé no bitmask (mesma ordem dos pinos D22..D31).
// Nomes conforme docs/pinout.md. Atenção: S10.1 (D27, ramo D) aparece como
// "S10.0" na tabela2_estados_cp.csv (Tabela II); S10.2 (D26) é o ramo C.
// ----------------------------------------------------------------------------
static const uint16_t CP_RELE_S7    = (1U << 0);  // D22 - Conexão com PE
static const uint16_t CP_RELE_S8    = (1U << 1);  // D23 - Falha 120R (Estado E)
static const uint16_t CP_RELE_S9_1  = (1U << 2);  // D24 - Bypass BX nominal
static const uint16_t CP_RELE_S9_2  = (1U << 3);  // D25 - Bypass BX limite inf.
static const uint16_t CP_RELE_S10_C = (1U << 4);  // D26 - S10.2: habilita ramo C
static const uint16_t CP_RELE_S10_D = (1U << 5);  // D27 - S10.1 (= S10.0 da Tabela II): habilita ramo D
static const uint16_t CP_RELE_S11_1 = (1U << 6);  // D28 - Bypass CX nominal
static const uint16_t CP_RELE_S11_2 = (1U << 7);  // D29 - Bypass CX limite inf.
static const uint16_t CP_RELE_S12_1 = (1U << 8);  // D30 - Bypass DX nominal
static const uint16_t CP_RELE_S12_2 = (1U << 9);  // D31 - Bypass DX limite inf.

static const uint16_t CP_RELES_NENHUM = 0U;

/**
 * @brief Retorna o bitmask de relés fechados para o estado (0 se inválido)
 */
uint16_t cpRelesDoEstado(CodigoEstadoCP estado);

/**
 * @brief Converte o argumento do comando "estado" (já em minúsculas e sem
 *        espaços nas pontas) no estado correspondente.
 *        Aceita: ax, bx+, bx, bx-, cx+, cx, cx-, dx+, dx, dx-, e
 *        e os aliases a, b, c, d (= nominal).
 * @return true se reconhecido (e grava em *saida); false caso contrário
 */
bool cpInterpretarEstado(const char *arg, CodigoEstadoCP *saida);

#endif // TABELA_ESTADOS_CP_H
