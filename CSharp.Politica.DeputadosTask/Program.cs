namespace CSharp.Politica.DeputadosTask
{
    using RestSharp;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using Data;

    public class GroupNameTree
    {
        public string Name { get; set; }
        public List<GroupNameTree> Childs = new List<GroupNameTree>();

        public GroupNameTree(string name)
        {
            this.Name = name;
        }

        public static explicit operator GroupNameTree(DictionaryTree<string, string> dictionaryTree)
        {
            var ret = new GroupNameTree(dictionaryTree.Data);

            foreach (var child in dictionaryTree)
            {
                ret.Childs.Add((GroupNameTree)child.Value);
            }

            return ret;
        }
    }

    public class DataDeputado
    {
        public DateTime Date { get; set; }
        public DictionaryTree<string, string> Group { get; set; }
        public decimal Value { get; set; }
        public string Parcela { get; set; }
    }

    public class Program
    {
        public static bool IsCnpj(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma;
            int resto;
            string digito;
            string tempCnpj;
            cnpj = cnpj.Trim();
            cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;
            tempCnpj = cnpj.Substring(0, 12);
            soma = 0;
            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];
            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cnpj.EndsWith(digito);
        }

        public static bool IsCpf(string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCpf;
            string digito;
            int soma;
            int resto;
            cpf = cpf.Trim();
            cpf = cpf.Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;
            tempCpf = cpf.Substring(0, 9);
            soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cpf.EndsWith(digito);
        }

        private static string treatIndex(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .ToUpper();
        }

        public static void Main(string[] args)
        {
            Func<string, string> treatIndexLocal = treatIndex;
            var year = 2018;
            var month = 11;

            GetData(out List<DataDeputado> datas, out DictionaryTree<string, string> groups, treatIndexLocal);
            //GroupBulkInsertByName(groups);
            //DataBulkInsert(datas, treatIndex);
        }

        private static void GetData(out List<DataDeputado> datas, out DictionaryTree<string, string> groups, Func<string, string> treatIndex)
        {
            datas = new List<DataDeputado>();
            groups = new DictionaryTree<string, string>(s => treatIndex(s), "Verba indenizatória");

            var xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.Load(@"C:\Users\MyUser\source\repos\CSharp.Data.Task\CSharp.Politica.DeputadosTask\CSharp.Politica.DeputadosTask\Dados.xml");

            var xpath = "xml/dados/despesa";
            var nodes = xmlDoc.SelectNodes(xpath);

            foreach (System.Xml.XmlNode childrenNode in nodes)
            {
                var datasCount = datas.Count;

                if (datasCount > 0 && datasCount % 5000 == 0)
                {
                    GroupBulkInsertByName(groups);
                    DataBulkInsert(datas, treatIndex);
                    datas = new List<DataDeputado>();
                    groups = new DictionaryTree<string, string>(s => treatIndex(s), "Verba indenizatória");
                }

                var dataEmissaoText = childrenNode.SelectSingleNode("dataEmissao").InnerText;
                var dataEmissao = string.IsNullOrWhiteSpace(dataEmissaoText) ? (DateTime?)null : DateTime.Parse(dataEmissaoText);
                var ano = int.Parse(childrenNode.SelectSingleNode("ano").InnerText);
                var mes = int.Parse(childrenNode.SelectSingleNode("mes").InnerText);
                var vlLiquidoText = childrenNode.SelectSingleNode("valorLiquido").InnerText;
                var vlLiquido = string.IsNullOrWhiteSpace(vlLiquidoText) ? (decimal?)null : decimal.Parse(vlLiquidoText, System.Globalization.CultureInfo.InvariantCulture);
                var vlDevolvidoText = childrenNode.SelectSingleNode("restituicao").InnerText;
                var vlDevolvido = string.IsNullOrWhiteSpace(vlDevolvidoText) ? (decimal?)null : decimal.Parse(vlDevolvidoText, System.Globalization.CultureInfo.InvariantCulture) * -1;
                var isCancelado = vlDevolvido.HasValue;
                //valorLiquido ?? restituicao * -1

                string tpDocumento;

                switch (childrenNode.SelectSingleNode("tipoDocumento").InnerText.Trim())
                {
                    case "0":
                        tpDocumento = "Tipo 0 (NotaFiscal) - ";
                        break;
                    case "1":
                        tpDocumento = "Tipo 1 (Recibo) - ";
                        break;
                    case "2":
                        tpDocumento = "Tipo 2 - ";
                        break;
                    case "3":
                        tpDocumento = "Tipo 3 - ";
                        break;
                    case "4":
                        tpDocumento = "Tipo 4 - ";
                        break;
                    default:
                        tpDocumento = "Tipo indefinido - ";
                        break;
                }

                var numeroDocumento = tpDocumento + childrenNode.SelectSingleNode("numero").InnerText;

                var group = groups.AddIfNew(
                    childrenNode.SelectSingleNode("siglaUF").InnerText,
                    childrenNode.SelectSingleNode("siglaPartido").InnerText,
                    childrenNode.SelectSingleNode("nomeParlamentar").InnerText,
                    childrenNode.SelectSingleNode("codigoLegislatura").InnerText.Trim(),
                    childrenNode.SelectSingleNode("descricao").InnerText,
                    childrenNode.SelectSingleNode("fornecedor").InnerText,
                    numeroDocumento,
                    isCancelado ? "Cancelado" : "Reembolsado");

                var data = new DataDeputado()
                {
                    Date = dataEmissao ?? new DateTime(ano, mes, 1),
                    Group = group,
                    Value = isCancelado ? vlDevolvido.Value * -1 : vlLiquido.Value,
                    Parcela = childrenNode.SelectSingleNode("parcela").InnerText
                };

                datas.Add(data);

                ////Console.WriteLine(childrenNode.SelectSingleNode("razao_social").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("nr_processo").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_orgao").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_orgao").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_unidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_unidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("categoria_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_categoria").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("grupo_despesa").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_grupo").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cd_modalidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("ds_modalidade").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("nr_mes").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("nr_ano").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("vl_pagamento").InnerText);
                ////Console.WriteLine(childrenNode.SelectSingleNode("cnpj").InnerText);
            }
        }

        private static void GroupBulkInsertByName(DictionaryTree<string, string> groups)
        {
            if (!groups.Any())
                return;

            var root = new string[] { "Política", "Deputados" };
            var body = new { NewGroups = (GroupNameTree)groups, RootPath = root };
            var request = new RestRequest(Method.POST)
            {
                Timeout = int.MaxValue,
                RequestFormat = DataFormat.Json
            };

            request.AddJsonBody(body);

            var url = "http://localhost:58994/odata/v4/groups/BulkInsertByName";
            var response = new RestClient(url)
                .Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.Content);
            }
        }

        private static void DataBulkInsert(List<DataDeputado> datas, Func<string, string> treatIndex)
        {
            if (!datas.Any())
                return;

            var dataUriStr = "http://localhost:58994/odata/v4";
            var dataUri = new Uri(dataUriStr);
            var container = new Default.Container(dataUri);
            container.Timeout = int.MaxValue;

            var deputadosGroupId = new Guid("25B8C2A1-3BC0-46D7-9222-5CBC4A466638");
            var groupsDbDictionary = container.Groups.ToDictionaryTree(g => treatIndex(g.Name), deputadosGroupId);
            var datas2 = datas
                .GroupBy(e => string.Join("/", e.Group.Key) + "/" + e.Date.ToString())
                .Select(eg =>
                    new Data.Models.DataDecimal()
                    {
                        CollectionDate = eg.First().Date,
                        GroupId = groupsDbDictionary[eg.First().Group.Key].Data.Id,
                        DecimalValue = eg.Sum(e => e.Value)
                    })
                .ToList<Data.Models.Data>();

            var bulkInsert = Default.ExtensionMethods.BulkInsert(container.Datas, datas2);
            bulkInsert.Execute();
        }
    }
}
