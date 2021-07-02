using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Leaf.xNet;

namespace PriceParser
{
    class Parser
    {
        private string link;
        private IHtmlDocument document;

        public Parser(string link)
        {
            Link = link;
        }

        public string Link
        {
            get
            {
                return null;
            }
            set
            {
                try
                {
                    HttpRequest request = new HttpRequest();
                    request.UserAgentRandomize();
                    string response = request.Get($"{value}").ToString();
                    HtmlParser htmlParser = new HtmlParser();
                    this.document = htmlParser.ParseDocument(response);

                    this.link = value;
                }
                catch (Leaf.xNet.HttpException)
                {
                    Console.Write("\nСайт не найден. Введите новую ссылку: ");
                    Link = Console.ReadLine();
                }
            }
        }


        public List<double> ReturnPrice()
        {
            var body = this.document.QuerySelectorAll("body *");

            // цены будем искать в TextContent и наименованиях классов и id тегов
            var priceContent = body.Where(item => item.TextContent.Contains("Цена") || item.TextContent.Contains("цена"));
            var priceClass = body.Where(item => item.ClassName != null && item.ClassName.Contains("price") || item.Id != null && item.Id.Contains("price"));

            // для двух ситуаций будем использовать два разных списака
            List<double> pricesInContext = new List<double>();
            List<double> pricesInClass = new List<double>();
            if (priceContent.Count() != 0)
            {
                priceContent = DeliteАncestor(priceContent); // метод удалит дочерние эдементы
                foreach (var element in priceContent)
                {
                    var bodyTags = body.ToArray();
                    for (int i = 0; i < bodyTags.Length; i++)
                    {
                        if (bodyTags[i] == element && i + 1 < bodyTags.Length)
                        {// исходя из рассматриваемых сайтов видно, что если в TextContent тега содержится слово "цена", то в следующем теге содержится ее численное представление
                            double number = ReturnNumber(bodyTags[i + 1].TextContent);
                            if (number != 0)
                                pricesInContext.Add(number);
                            break;
                        }
                    }

                }
            }
            if (priceClass.Count() != 0)
            {
                priceClass = DeliteАncestor(priceClass);
                foreach (var el in priceClass)
                {
                    double number = ReturnNumber(el.TextContent); // исходя из рассматриваемых сайтов видно, что если имя класса, или id тега содержит "price", то TextContent данного тега может содержать численное представление цены
                    if (number != 0)
                        if (el.Id != null && el.Id.Contains("result") || el.ClassName != null && el.ClassName.Contains("result")) // если имя класса, или id тега содержит "result", то тег содержит цену основного товара представленного на странице
                        {
                            pricesInClass.Insert(0, number);
                        }
                        else
                        {
                            pricesInClass.Add(number);
                        }
                }
            }
            List<double> prices = new List<double>();
            if (pricesInContext.Count() == 1) // если один из рассматриваемых слачаев содержит только одну цену, то эта цена пренадлежит основному товару представленному на странице
            {
                prices.Add(pricesInContext[0]);
            }
            else if (pricesInClass.Count() == 1)
            {
                prices.Add(pricesInClass[0]);
            }
            else
            {
                prices.AddRange(pricesInContext);
                prices.AddRange(pricesInClass);
                prices = prices.Distinct().ToList();
            }
            return prices;
        }


        private double ReturnNumber(string str)
        {
            double price = default(double);
            if (str.Length < 100) // если текст тега содержит более 100 символов, то скорее всего помимо цены в нем содержится много лишней информации из которой будет сложно вычленить цену 
            {
                List<string> numbers = new List<string>();
                str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "").Trim();
                for (int i = 0; i < str.Length; i++)
                {
                    string number = default(string);
                    while (char.IsDigit(str[i]) || str[i] == '.' || str[i] == ',')
                    {
                        number += str[i];
                        if (i < str.Length - 1)
                            i++;
                        else
                            break;
                    }
                    numbers.Add(number);
                }
                if (numbers.Count() != 0)
                {
                    try
                    {
                        numbers.Sort();
                        IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };
                        price = Convert.ToDouble(numbers[numbers.Count() - 1], formatter); // за цену возьмем самое большое найденное число
                    }
                    catch (System.FormatException)
                    {
                        price = 0;
                    }
                }
            }
            return price;
        }


        private IEnumerable<AngleSharp.Dom.IElement> DeliteАncestor(IEnumerable<AngleSharp.Dom.IElement> elements)
        {
            List<AngleSharp.Dom.IElement> list = elements.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].TextContent == "")
                    {
                        list.Remove(list[j]);
                        j--;
                        continue;
                    }
                    if (list[i].TextContent.Contains(list[j].TextContent) && list[i] != list[j])
                    {
                        list.Remove(list[i]);
                        i--;
                        break;
                    }
                }
            }
            return list;
        }

    }
}
