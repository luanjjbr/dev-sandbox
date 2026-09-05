using System;
using System.Collections.Generic;
using System.Text;

namespace dev_sandbox.Core
{
    public class CalculadoraHex
    {
        public int HexParaDecimal(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                throw new ArgumentException("O valor hexadecimal não pode ser vazio.");
            }

            return Convert.ToInt32(hex, 16);
        }
    }
}
