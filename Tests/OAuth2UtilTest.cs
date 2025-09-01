/*
 * *
 *  * Copyright 2019 eBay Inc.
 *  *
 *  * Licensed under the Apache License, Version 2.0 (the "License");
 *  * you may not use this file except in compliance with the License.
 *  * You may obtain a copy of the License at
 *  *
 *  *  http://www.apache.org/licenses/LICENSE-2.0
 *  *
 *  * Unless required by applicable law or agreed to in writing, software
 *  * distributed under the License is distributed on an "AS IS" BASIS,
 *  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  * See the License for the specific language governing permissions and
 *  * limitations under the License.
 *  *
 */
using Microsoft.Extensions.DependencyInjection;
// File: EbayOAuth/EbayOAuthClient.cs


namespace PolyhydraGames.API.Ebay.Tests;
public class OAuth2UtilTest : TestBase<OAuth2UtilTest>
{
    public OAuth2UtilTest()
    {
        base.BuildServiceProvider(x =>
        {
            x.AddHttpClient();
            x.AddSingleton<PolyhydraGames.API.Ebay.EbayOAuthClient>();
        });

        Credentials = ServiceProvider.GetService<OAuthCreds>();
    }
    public OAuthCreds Credentials { get; set; }


}