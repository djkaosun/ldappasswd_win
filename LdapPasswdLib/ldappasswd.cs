using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using mylib.CLI;

namespace LdapPasswdLib
{
    class ldappasswd
    {
        /// <summary>
        /// コンフィグのファイル名。
        /// </summary>
        public const string CONFIG_FILE_NAME = "ldappasswd_config.json";

        /// <summary>
        /// LDAPS のポート番号
        /// </summary>
        public const int LDAP_PORT = 389;
        public const int LDAPS_PORT = 636;

        private const string PRESS_ENTER_KEY_MESSAGE = "続行するには Enter キーを押してください . . .";

        static void Main(string[] args)
        {
            // コマンドの .exe が置かれたフォルダーのフルパスを取得
            var fullPath = Directory.GetParent(Process.GetCurrentProcess().MainModule.FileName);

            IDictionary<string, string[]> configOptions = new Dictionary<string, string[]>();
            // 設定ファイルの読み込み
            try
            {
                configOptions = LoadConfig(fullPath + "\\" + CONFIG_FILE_NAME);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("コンフィグの書式が間違っています。");
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine("--------");
                Console.Error.WriteLine(e);
                Console.Write(PRESS_ENTER_KEY_MESSAGE);
                Console.ReadLine();
                Environment.Exit(-1);
            }

            IDictionary<string, IList<string>> options = null;
            try
            {
                // パース処理。
                options = ParseArgs(args, configOptions);
            }
            catch (Exception e)
            {
                // e のメッセージにはコマンドのヘルプが格納されている。
                Console.Error.WriteLine(e);
                Console.Write(PRESS_ENTER_KEY_MESSAGE);
                Console.ReadLine();
                Environment.Exit(-1);
            }

            // SSL/TLS を利用するか否か
            var isTls = false;
            if (options.ContainsKey("tls"))
            {
                if (options["tls"].Count < 1 || options["tls"][0] != "off")
                {
                    isTls = true;
                }
            }
            Console.WriteLine("tls: " + isTls + ", ");

            // サーバー ポートの明示
            options["server"][0] = ServerPortSpecify(options["server"][0], isTls);

            // オプションの表示
            foreach (KeyValuePair<string, IList<string>> option in options)
            {
                var optionName = option.Key;
                if (optionName != "tls")
                {
                    Console.Write(optionName + ": ");
                    foreach (string value in option.Value)
                    {
                        Console.Write(value + ", ");
                    }
                    Console.WriteLine();
                }
            }

            // ユーザー名の手動入力 (オプションで未指定の時)
            InputValue("dn", ref options);

            // パスワードの手動入力 (オプションで未指定の時)
            InputValue("oldpassword", ref options);

            // パスワードの手動入力 (オプションで未指定の時)
            InputValue("newpassword", ref options);

            try
            {
                // 実処理のメソッド呼び出し
                LdapPasswordChanger.ChangePassword(options["dn"][0], options["oldpassword"][0], options["newpassword"][0], options["server"][0], isTls);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.Write(PRESS_ENTER_KEY_MESSAGE);
                Console.ReadLine();
                Environment.Exit(-1);
            }

            // 実行後の待機
            Console.Write(PRESS_ENTER_KEY_MESSAGE);
            Console.ReadLine();
        }

        /// <summary>
        /// コマンドライン引数をパースして、オプション ディクショナリを組み立てます。
        /// </summary>
        /// <param name="args">コマンドライン引数。</param>
        /// <param name="configOptions">コンフィグから読み込んだオプション。</param>
        /// <returns>パース結果のオプション ディクショナリ。</returns>
        private static IDictionary<string, IList<string>> ParseArgs(string[] args, IDictionary<string, string[]> configOptions)
        {
            // コマンドライン オプションのシンタックス。
            OptionSyntaxDictionary syntaxes = new OptionSyntaxDictionary {
                new OptionSyntax("server", "LDAP サーバーのホスト名または IP アドレス。", 1, 1, new char[] { 's' }, false, true, true),
                new OptionSyntax("tls", "LDAPS にする。コンフィグ ファイルで tls を指定した場合も、「--tls off」で LDAP 接続できます。", 0, 1, new char[] { 't' },false,false,false,new System.Text.RegularExpressions.Regex("^(on|off)$")),
                new OptionSyntax("dn", "ユーザー名。", 1, 1, new char[] { 'd' }),
                new OptionSyntax("oldpassword", "ユーザー エントリのある DN。", 1, 1, new char[] { 'o' }),
                new OptionSyntax("newpassword", "パスワード。", 1, 1, new char[] { 'n' })
            };

            // パーサー組み立て
            var argsParser = new ArgsParser
            {
                OptionSyntaxDictionary = syntaxes,
                ConfigOptionDictionary = configOptions
            };

            IDictionary<string, IList<string>> options = null;
            try
            {
                // パース処理。
                options = argsParser.Parse(args);
            }
            catch (Exception e)
            {
                // パースに失敗したら、ヘルプと元の Exception の文字列をいれた Exception を投げる
                throw new Exception(syntaxes.GetHelp() + "\n" + e);
            }

            return options;
        }

        /// <summary>
        /// オプションで欠落している値をユーザーに問い合わせます。
        /// </summary>
        /// <param name="optionName">オプション名。</param>
        /// <param name="options">これまでのオプションを格納したディクショナリ。ユーザー問い合わせ結果はこのディクショナリーに追加で埋められます。</param>
        private static void InputValue(string optionName, ref IDictionary<string, IList<string>> options)
        {
            if (!options.ContainsKey(optionName) || options[optionName] == null || options[optionName][0].Length < 1)
            {
                Console.Write(optionName + ": ");
                string server = Console.ReadLine();
                if (server.Length != 0)
                {
                    options[optionName] = new string[] { server };
                }
                else
                {
                    options[optionName] = new string[] { "" };
                }
            }
        }

        /// <summary>
        /// 結果を出力します。
        /// </summary>
        /// <param name="searchResultEntry">ログインに成功したユーザー。</param>
        private static void OutputResult(SearchResultEntry searchResultEntry)
        {
            if (searchResultEntry != null)
            {
                Console.WriteLine("[[[ authn succeeded. ]]]");
                Console.WriteLine();
                foreach (DictionaryEntry dictionaryEntry in searchResultEntry.Attributes)
                {
                    DirectoryAttribute directoryAttribute = dictionaryEntry.Value as DirectoryAttribute;
                    // 属性名の出力
                    Console.Write(directoryAttribute.Name + ": ");
                    foreach (string valueString in directoryAttribute.GetValues(typeof(string)))
                    {
                        // 雑にすべての値を文字列として出力するので、内容によっては文字化ける。
                        Console.Write(valueString + ", "); ;
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("[[[ authn failed. ]]]");
            }
        }

        /// <summary>
        /// 設定ファイルを読み込みます。
        /// </summary>
        /// <param name="filePath">設定ファイルのファイル パス。</param>
        /// <returns>設定ファイルを読み込んだ結果のディクショナリ。</returns>
        private static IDictionary<string, string[]> LoadConfig(string filePath)
        {
            var options = new Dictionary<string, string[]>();
            if (!File.Exists(filePath)) return options;

            StreamReader sr = new StreamReader(filePath);
            var configString = sr.ReadToEnd();
            sr.Close();

            return JsonSerializer.Deserialize<Dictionary<string, string[]>>(configString);
        }

        /// <summary>
        /// TCP ポート指定なし、かつ、LDAPS のときはポート指定「:636」を追加する。
        /// </summary>
        /// <param name="ldapServer">LDAP サーバー指定</param>
        private static string ServerPortSpecify(string ldapServer, bool isTls)
        {
            if (!Regex.IsMatch(ldapServer, ":[1-9][0-9]*$"))
            {
                if (isTls) return ldapServer + ":" + LDAPS_PORT;
                else return ldapServer + ":" + LDAP_PORT;
            }
            else return ldapServer;
        }
    }
}
