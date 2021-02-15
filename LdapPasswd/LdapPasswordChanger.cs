﻿using System;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.RegularExpressions;

namespace LdapPasswdLib
{
    public static class LdapPasswordChanger
    {
        public const int LDAPS_DEFAULT_PORT = 3296;

        public const string LDAP_EXOP_MODIFY_PASSWD = "1.3.6.1.4.1.4203.1.11.1";
        //public const int LBER_USE_DER = 0x01;
        public const int LDAP_TAG_EXOP_MODIFY_PASSWD_ID = 0x80;
        public const int LDAP_TAG_EXOP_MODIFY_PASSWD_OLD = 0x81;
        public const int LDAP_TAG_EXOP_MODIFY_PASSWD_NEW = 0x82;

        /// <summary>
        /// LDAP でパスワードを変更する。
        /// </summary>
        /// <param name="accountDN">アカウントの DN</param>
        /// <param name="oldPasswd">現在のパスワード</param>
        /// <param name="newPasswd">変更後のパスワード</param>
        /// <param name="ldapServer">LDAP サーバーのホスト名または IP アドレス</param>
        /// <param name="isTls">LDAPS にする場合 true。LDAP のままにする場合 false。</param>
        public static void ChangePassword(string accountDN, string oldPasswd, string newPasswd, string ldapServer, bool isTls)
        {
            if (accountDN == null || oldPasswd == null || newPasswd == null || ldapServer == null)
                    throw new ArgumentNullException();

            if (accountDN.Length < 1 || oldPasswd.Length < 1 || newPasswd.Length < 1 || ldapServer.Length < 1)
                    throw new ArgumentException();

            ldapServer = ServerPortSpecify(ldapServer, isTls);

            LdapConnection ldapConnection = new LdapConnection(ldapServer)
            {
                Credential = new NetworkCredential(accountDN, oldPasswd),
                AuthType = AuthType.Basic,
                Timeout = new TimeSpan(0, 0, 10)
            };
            ldapConnection.SessionOptions.ProtocolVersion = 3;
            ldapConnection.SessionOptions.SecureSocketLayer = isTls;

            // https://tools.ietf.org/html/rfc3062
            var ber = BerConverter.Encode("{tststs}",
                    LDAP_TAG_EXOP_MODIFY_PASSWD_ID, accountDN,
                    LDAP_TAG_EXOP_MODIFY_PASSWD_OLD, oldPasswd,
                    LDAP_TAG_EXOP_MODIFY_PASSWD_NEW, newPasswd);
            var modifyPasswdRequest = new ExtendedRequest(LDAP_EXOP_MODIFY_PASSWD, ber);
            try
            {
                // 認証したいユーザーでバインドする
                ldapConnection.Bind();

                // パスワード変更要求を送信
                var modifyPasswdResponse = (ExtendedResponse)ldapConnection.SendRequest(modifyPasswdRequest);

                // 応答が「成功」か確認
                if (modifyPasswdResponse.ResultCode != ResultCode.Success)
                        throw new Exception("Could not change password. (" + modifyPasswdResponse.ResultCode + ")");
            }
            finally
            {
                if (ldapConnection != null)
                {
                    try
                    {
                        ldapConnection.Dispose();
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }
                }
            }
        }

        /// <summary>
        /// TCP ポート指定なし、かつ、LDAPS のときはポート指定を追加する。
        /// </summary>
        /// <param name="ldapServer">LDAP サーバー指定</param>
        private static string ServerPortSpecify(string ldapServer, bool isTls)
        {
            if (isTls && !Regex.IsMatch(ldapServer, ":[1-9][0-9]*$")) return ldapServer + ":" + LDAPS_DEFAULT_PORT;
            else return ldapServer;
        }
    }
}