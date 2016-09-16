﻿/*
 * Copyright 2016 Luděk Rašek and other contributors as 
 * indicated by the @author tags.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using openeet_lite;

namespace tests
{
    class Program
    {

        public static void simpleRegistrationProcessTest()
        {
            // moznost pouziti
            /*EetRegisterRequest request = EetRegisterRequest.Builder()
               .SetDicPopl("CZ1212121218")
               .SetIdProvoz("1")
               .SetIdPokl("POKLADNA01")
               .SetPoradCis("1")
               .SetDatTrzbys("2016-09-12T08:43:28+02:00")
               .SetCelkTrzba(100.0)
               .SetRezim(0)
               .SetPkcs12(TestData._01000003)
               .SetPkcs12Password("eet")
               .Build();*/


            EetRegisterRequest request = new EetRequestBuilder()
            {
                DicPopl = "CZ1212121218",
                IdProvoz = "1",
                IdPokl = "POKLADNA01",
                PoradCis = "1",
                DatTrzby = DateTime.Now,
                CelkTrzba = 100.0,
                Rezim = 0,
                Pkcs12 = TestData._01000003,
                Pkcs12Password = "eet"
            }.Build();

            //for receipt printing in online mode
            string bkp = request.FormatBkp();
            if (bkp == null) throw new ApplicationException("BKP is null");

            //for receipt printing in offline mode
            string pkp = request.FormatPkp();
            if (pkp == null) throw new ApplicationException("PKP is null");
            //the receipt can be now stored for offline processing

            //try send
            string requestBody = request.GenerateSoapRequest();
            if (requestBody == null) throw new ApplicationException("SOAP request is null");
            string response = request.SendRequest(requestBody, "https://pg.eet.cz:443/eet/services/EETServiceSOAP/v3");

            //via local stunnel
            //string  response = request.SendRequest(requestBody, "http://127.0.0.1:27541/eet/services/EETServiceSOAP/v2");

            // TODO
            //zde by to chtelo dodelat kontrolu jestli prijata zprava nebyla zmenena, jestli souhlasi podpis zpravy
            //mauzete to nekdo doplnit ?


            //extract FIK
            if (response == null) throw new ApplicationException("response is null");
            if (response.IndexOf("Potvrzeni fik=") < 0) throw new ApplicationException("FIK not found in the response");
            //ready to print online receipt
            Console.WriteLine("OK!"); //a bit brief :-) but enough
                                      //set minimal business data & certificate with key loaded from pkcs12 file
        }

        /*
            <Data dic_popl="CZ1212121218" id_provoz="1" id_pokl="POKLADNA01" porad_cis="1" dat_trzby="2016-06-30T08:43:28+02:00" celk_trzba="100.00" rezim="0"/>
            <pkp cipher="RSA2048" digest="SHA256" encoding="base64">Ddk2WTYu8nzpQscH7t9n8cBsGq4k/ggCwdfkPjM+gHUHPL8P7qmnWofzeW2pAekSSmOClBjF141yN+683g0aXh6VvxY4frBjYhy4XB506LDykIW0oAv086VH7mR0utA8zGd7mCI55p3qv1M/oog/2yG0DefD5mtHIiBG7/n7jgWbROTatJPQYeQWEXEoOJh9/gAq2kuiK3TOYeGeHwOyFjM2Cy3UVal8E3LwafP49kmGOWjHG+cco0CRXxOD3b8y4mgBqTwwC4V8e85917e5sVsaEf3t0hwPkag+WM1LIRzW+QwkkgiMEwoIqCAkhoF1eq/VcsML2ZcrLGejAeAixw==</pkp>
            <bkp digest="SHA1" encoding="base16">AC502107-1781EEE4-ECFD152F-2ED08CBA-E6226199</bkp>
            */
        public static void signAndSend()
        {
            EetRegisterRequest data = EetRegisterRequest.Builder()
               .SetDicPopl("CZ1212121218")
               .SetIdProvoz("1")
               .SetIdPokl("POKLADNA01")
               .SetPoradCis("1")
               .SetDatTrzbys("2016-09-12T08:43:28+02:00")
               .SetCelkTrzba(100.0)
               .SetPkcs12(TestData._01000003)
               .SetPkcs12Password("eet")
               .SetRezim(0)
               .Build();
            if (data == null)
                throw new Exception("failed - data null");
            Console.WriteLine("business data created");

            string pkp = EetRegisterRequest.FormatPkp(data.Pkp);
            string bkp = EetRegisterRequest.FormatBkp(data.Bkp);
            string expectedPkp = "Ddk2WTYu8nzpQscH7t9n8cBsGq4k/ggCwdfkPjM+gHUHPL8P7qmnWofzeW2pAekSSmOClBjF141yN+683g0aXh6VvxY4frBjYhy4XB506LDykIW0oAv086VH7mR0utA8zGd7mCI55p3qv1M/oog/2yG0DefD5mtHIiBG7/n7jgWbROTatJPQYeQWEXEoOJh9/gAq2kuiK3TOYeGeHwOyFjM2Cy3UVal8E3LwafP49kmGOWjHG+cco0CRXxOD3b8y4mgBqTwwC4V8e85917e5sVsaEf3t0hwPkag+WM1LIRzW+QwkkgiMEwoIqCAkhoF1eq/VcsML2ZcrLGejAeAixw==";
            string expectedBkp = "AC502107-1781EEE4-ECFD152F-2ED08CBA-E6226199";
            if (!pkp.Equals(expectedPkp))
                throw new Exception("failed - PKP differs");
            if (!bkp.Equals(expectedBkp))
                throw new Exception("failed - BKP differs");
            Console.WriteLine("Codes validated");

            string signed = data.GenerateSoapRequest();
            Console.WriteLine("SOAP request created");

            //assertTrue(validateXmlDSig(signed, data.getCertificate()));
            string response = data.SendRequest(signed, "https://pg.eet.cz:443/eet/services/EETServiceSOAP/v3");

            //via local stunnel 
            //string  response=data.SendRequest(signed, "http://127.0.0.1:27541/eet/services/EETServiceSOAP/v2");

            if (response.IndexOf("Potvrzeni fik=") < 0) throw new ApplicationException("FIK not found in the response");
            Console.WriteLine("FIK received:" + response.Substring(response.IndexOf("Potvrzeni fik=") + 15, 36));
        }

        static void Main(string[] args)
        {
            //signAndSend();
            simpleRegistrationProcessTest();
            Console.WriteLine("Press any key to finish ...");
            Console.ReadKey();
        }
    }
}


