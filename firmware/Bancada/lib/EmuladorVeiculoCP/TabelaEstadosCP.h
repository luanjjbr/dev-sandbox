/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: TabelaEstadosCP.h
 * Finalidade: Lógica pura do Piloto de Controle (CP - IEC 61851-1 Anexo A):
 *             enum dos 11 estados emulados pelo veículo, tabela estado ->
 *             relés (bitmask) e parser do argumento do comando "estado".
 *             Sem dependência do Arduino (verificável em tempo de compilação).
 * Fonte da tabela: esquemático EasyEDA bancada_de_teste_reles_corrigido
 *                  (placa real), tabela2_estados_cp.csv, docs/pinout.md
 * ============================================================================
 */

#ifndef TABELA_ESTADOS_CP_H
#define TABELA_ESTADOS_CP_H

#include <stdint.h>

/**
 * @brief Os 11 estados que o veículo emula no CP (Tabela II da IEC 61851-1).
 */
enum class CodigoEstadoCP : uint8_t {
    AX_NOMINAL = 0,  // 12 V - Veículo não conectado (K_PE/S7 aberto)
    BX_SUPERIOR,     // 10 V - Conectado (standby), limite superior
    BX_NOMINAL,      //  9 V - Conectado (standby), nominal
    BX_INFERIOR,     //  8 V - Conectado (standby), limite inferior
    CX_SUPERIOR,     //  7 V - Carga sem ventilação, limite superior
    CX_NOMINAL,      //  6 V - Carga sem ventilação, nominal
    CX_INFERIOR,     //  5 V - Carga sem ventilação, limite inferior
    DX_SUPERIOR,     //  4 V - Carga com ventilação, limite superior
    DX_NOMINAL,      //  3 V - Carga com ventilação, nominal
    DX_INFERIOR,     //  2 V - Carga com ventilação, limite inferior
    E_FALHA          //  1 V - Curto CP-PE antes do diodo (K_FALHA/S8)
};

static const uint8_t CP_TOTAL_ESTADOS = 11;
static const uint8_t CP_TOTAL_RELES   = 10;

// ----------------------------------------------------------------------------
// Bit de cada RELÉ FÍSICO da placa (esquemático EasyEDA
// bancada_de_teste_reles_corrigido, 2026-10-09). Cada ramo é uma ÁRVORE:
// um relé "habilita" (EN) leva o CPV ao comum de um relé "seletor" (SEL), cujo
// NC vai ao tap nominal e o NO ao tap inferior. Sem o EN, o SEL não faz nada.
//   bit  pino  relé    função                       contatos usados
//   0    D30   RLY6    K_PE   (S7)  liga o retorno ao PE     C-NO
//   1    D31   RLY1    K_FALHA(S8)  CP -> R29 120R -> retorno C-NO (antes do diodo)
//   2    D29   RLY7    K_B_EN       CPV -> comum do K_B_SEL  C-NO
//   3    D28   RLY2    K_B_SEL      NC: abaixo de R30 (2740R) / NO: abaixo de R32 (1820R)
//   4    D27   RLY5    K_CD_EN      CPV -> comum do K_CD_SEL C-NO
//   5    D26   RLY8    K_CD_SEL     NC: topo do ramo C / NO: topo do ramo D
//   6    D25   RLY3    K_C_EN       topo C -> comum do K_C_SEL C-NO
//   7    D24   RLY4    K_C_SEL      NC: abaixo de R39 (1301R) / NO: abaixo de R37 (909R)
//   8    D23   RLY9    K_D_EN       topo D -> comum do K_D_SEL C-NO
//   9    D22   RLY10   K_D_SEL      NC: abaixo de R46 (270R) / NO: abaixo de R51 (140R)
// Ordem dos pinos no construtor de EmuladorVeiculoCP = ordem dos bits.
// ----------------------------------------------------------------------------
static const uint16_t CP_K_PE     = (1U << 0);  // D30 - S7: conexão com PE
static const uint16_t CP_K_FALHA  = (1U << 1);  // D31 - S8: falha 120R (Estado E)
static const uint16_t CP_K_B_EN   = (1U << 2);  // D29 - habilita bypass do ramo B
static const uint16_t CP_K_B_SEL  = (1U << 3);  // D28 - seletor B (0 = nominal, 1 = inferior)
static const uint16_t CP_K_CD_EN  = (1U << 4);  // D27 - habilita a carga (ramos C/D)
static const uint16_t CP_K_CD_SEL = (1U << 5);  // D26 - seletor de ramo (0 = C, 1 = D)
static const uint16_t CP_K_C_EN   = (1U << 6);  // D25 - habilita bypass do ramo C
static const uint16_t CP_K_C_SEL  = (1U << 7);  // D24 - seletor C (0 = nominal, 1 = inferior)
static const uint16_t CP_K_D_EN   = (1U << 8);  // D23 - habilita bypass do ramo D
static const uint16_t CP_K_D_SEL  = (1U << 9);  // D22 - seletor D (0 = nominal, 1 = inferior)

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
