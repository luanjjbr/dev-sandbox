/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: SaidaSerial.cpp
 * Finalidade: Implementação da saída da UART0 por polling (ver SaidaSerial.h)
 * ============================================================================
 */

#include "SaidaSerial.h"
#include <Arduino_FreeRTOS.h>
#include <task.h>

SaidaSerial Saida;

size_t SaidaSerial::write(uint8_t c)
{
    // Espera o registrador de dados livre. Com o escalonador rodando, cede a
    // CPU às tasks de mesma prioridade (a cada ~1 ms por byte a 9600 baud);
    // antes dele (setup), apenas espera.
    while ((UCSR0A & _BV(UDRE0)) == 0U)
    {
        if (xTaskGetSchedulerState() == taskSCHEDULER_RUNNING)
        {
            taskYIELD();
        }
    }
    UDR0 = c;
    return 1;
}
