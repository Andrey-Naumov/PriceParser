using System;
using System.Collections.Generic;


namespace PriceParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string link;
            {
                Console.Write("Введите ссылку: ");
                link = Console.ReadLine();
            }

            Parser parser = new Parser(link);
            List<double> price = parser.ReturnPrice();
            {
                Console.Write("Цены: ");
                foreach (var element in price)
                {
                    Console.Write(element + "  ");
                }
            }
            Console.ReadKey();
        }
    }
}
