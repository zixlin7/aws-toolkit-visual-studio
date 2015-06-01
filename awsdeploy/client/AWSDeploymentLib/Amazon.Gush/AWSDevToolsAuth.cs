/*
 * Copyright 2011-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Amazon.DevTools
{
    public class AWSDevToolsAuth
    {
        const string AWS_ALGORITHM = "HMAC-SHA256";
        const string HASH_ALGORITHM = "SHA-256";
        const string HMAC_ALGORITHM = "HMACSHA256";
        const string SCHEME = "AWS4";
        const string TERMINATOR = "aws4_request";

        AWSUser user;
        AWSDevToolsRequest request;

        public AWSDevToolsAuth(AWSUser user, AWSDevToolsRequest request)
        {
            this.user = user;
            this.request = request;
        }

        static byte[] DeriveKey(AWSUser user, AWSDevToolsRequest request)
        {
            string secret = string.Format("{0}{1}", AWSDevToolsAuth.SCHEME, user.SecretKey);
            byte[] kSecret = Encoding.UTF8.GetBytes(secret);
            byte[] kDate = AWSDevToolsAuth.Hash(AWSDevToolsAuth.HMAC_ALGORITHM, kSecret, Encoding.UTF8.GetBytes(request.DateStamp));
            byte[] kRegion = AWSDevToolsAuth.Hash(AWSDevToolsAuth.HMAC_ALGORITHM, kDate, Encoding.UTF8.GetBytes(request.Region));
            byte[] kService = AWSDevToolsAuth.Hash(AWSDevToolsAuth.HMAC_ALGORITHM, kRegion, Encoding.UTF8.GetBytes(request.Service));
            byte[] key = AWSDevToolsAuth.Hash(AWSDevToolsAuth.HMAC_ALGORITHM, kService, Encoding.UTF8.GetBytes(AWSDevToolsAuth.TERMINATOR));
            return key;
        }

        public string DerivePassword()
        {
            this.user.Validate();
            this.request.Validate();

            string signature = AWSDevToolsAuth.SignRequest(this.user, this.request);
            string password = string.Format("{0}Z{1}", this.request.DateTimeStamp, signature);
            return password;
        }

        public Uri DeriveRemote()
        {
            this.request.Validate();

            string path = this.request.DerivePath();
            string password = this.DerivePassword();
            string username = this.DeriveUserName();
            UriBuilder remote = new UriBuilder()
            {
                Host = this.request.Host,
                Path = path,
                Password = password,
                Scheme = "https",
                UserName = username,
            };
            return remote.Uri;
        }

        public string DeriveUserName()
        {
            this.user.Validate();

            return this.user.AccessKey;
        }

        static byte[] Hash(string algorithm, byte[] message)
        {
            HashAlgorithm hash = HashAlgorithm.Create(algorithm);
            byte[] digest = hash.ComputeHash(message);
            return digest;
        }

        static byte[] Hash(string algorithm, byte[] key, byte[] message)
        {
            KeyedHashAlgorithm hash = KeyedHashAlgorithm.Create(algorithm);
            hash.Key = key;
            byte[] digest = hash.ComputeHash(message);
            return digest;
        }

        static string SignRequest(AWSUser user, AWSDevToolsRequest request)
        {
            string scope = string.Format("{0}/{1}/{2}/{3}", request.DateStamp, request.Region, request.Service, AWSDevToolsAuth.TERMINATOR);
            StringBuilder stringToSign = new StringBuilder();
            stringToSign.AppendFormat("{0}-{1}\n{2}\n{3}\n", AWSDevToolsAuth.SCHEME, AWSDevToolsAuth.AWS_ALGORITHM, request.DateTimeStamp, scope);
            byte[] requestBytes = Encoding.UTF8.GetBytes(request.DeriveRequest());
            byte[] requestDigest = AWSDevToolsAuth.Hash(AWSDevToolsAuth.HASH_ALGORITHM, requestBytes);
            stringToSign.Append(AWSDevToolsAuth.ToHex(requestDigest));
            byte[] key = AWSDevToolsAuth.DeriveKey(user, request);
            byte[] digest = AWSDevToolsAuth.Hash(AWSDevToolsAuth.HMAC_ALGORITHM, key, Encoding.UTF8.GetBytes(stringToSign.ToString()));
            string signature = AWSDevToolsAuth.ToHex(digest);
            return signature;
        }

        static string ToHex(byte[] data)
        {
            StringBuilder hex = new StringBuilder();
            foreach (byte b in data)
            {
                hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            return hex.ToString();
        }
    }
}
