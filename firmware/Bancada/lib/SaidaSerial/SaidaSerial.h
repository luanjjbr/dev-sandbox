/**
 * ============================================================================
 * Projeto: Bancada
 * Arquivo: SaidaSerial.h
 * Finalidade: Saída de texto da UART0 por polling do UDRE (sem a ISR de TX da
 *             HardwareSerial), cedendo a CPU às outras tasks entre os bytes.
 * Microcontroladores: ATmega328P (Nano) e ATmega2560 (Mega)
 * ============================================================================
 *
 * Por que existe (2026-11-02): no SimulIDE (1.1.0-SR2 e 2.0), com FreeRTOS, o
 * MCU simulado travava sempre que uma resposta passava dos 64 bytes do buffer
 * de TX da HardwareSerial (`estado status`, `cabo status`, `help`): saíam 1-2
 * bytes e o firmware parava no laço "buffer cheio" de HardwareSerial::write,
 * esperando uma interrupção UDRE que o simulador não voltava a gerar. O mesmo
 * texto sem FreeRTOS, ou escrito por polling, funciona.
 *
 * No AVR real a interrupção UDRE é por NÍVEL (dispara enquanto UDRE=1 e
 * UDRIE=1), então a HardwareSerial também funcionaria; escrever por polling é
 * igualmente correto no hardware: mesma taxa (9600 baud) e a task que imprime
 * cede a CPU (taskYIELD) enquanto o registrador de dados não está livre.
 *
 * Uso: Saida.print(...) / Saida.println(...) / Saida.write(...).
 * A RECEPÇÃO continua na Serial (Serial.begin/available/read).
 */

#ifndef SAIDA_SERIAL_H
#define SAIDA_SERIAL_H

#include <Arduino.h>

class SaidaSerial : public Print {
public:
    size_t write(uint8_t c) override;
    using Print::write;
};

extern SaidaSerial Saida;

#endif // SAIDA_SERIAL_H
