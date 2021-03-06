﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using FluentAssertions;
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.UnitTests.Common;
using IdentityServer4.Validation;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Xunit;
using IdentityServer.UnitTests.Common;

namespace IdentityServer4.UnitTests.ResponseHandling
{
    public class AuthorizeInteractionResponseGeneratorTests_Login
    {
        IdentityServerOptions _options = new IdentityServerOptions();
        AuthorizeInteractionResponseGenerator _subject;
        MockConsentService _mockConsentService = new MockConsentService();

        public AuthorizeInteractionResponseGeneratorTests_Login()
        {
            _subject = new AuthorizeInteractionResponseGenerator(
                new StubClock(),
                TestLogger.Create<AuthorizeInteractionResponseGenerator>(),
                _mockConsentService,
                new MockProfileService());
        }

        [Fact]
        public async Task Anonymous_User_must_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = Principal.Anonymous
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_must_not_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client(),
                Subject = IdentityServerPrincipal.Create("123", "dom")
            };

            var result = await _subject.ProcessInteractionAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_allowed_current_Idp_must_not_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = IdentityServerPrincipal.Create("123", "dom"),
                Client = new Client 
                {
                    IdentityProviderRestrictions = new List<string> 
                    {
                        IdentityServerConstants.LocalIdentityProvider
                    }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_restricted_current_Idp_must_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = IdentityServerPrincipal.Create("123", "dom"),
                Client = new Client
                {
                    IdentityProviderRestrictions = new List<string> 
                    {
                        "some_idp"
                    }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_with_allowed_requested_Idp_must_not_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client(),
                 AuthenticationContextReferenceClasses = new List<string>{
                    "idp:" + IdentityServerConstants.LocalIdentityProvider
                },
                Subject = IdentityServerPrincipal.Create("123", "dom")
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_different_requested_Idp_must_SignIn()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client(),
                AuthenticationContextReferenceClasses = new List<string>{
                    "idp:some_idp"
                },
                Subject = IdentityServerPrincipal.Create("123", "dom")
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task locally_authenticated_user_but_client_does_not_allow_local_should_sign_in()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Client = new Client()
                {
                    EnableLocalLogin = false
                },
                Subject = IdentityServerPrincipal.Create("123", "dom")
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task prompt_login_should_sign_in()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = IdentityServerPrincipal.Create("123", "dom"),
                PromptMode = OidcConstants.PromptModes.Login,
                Raw = new NameValueCollection()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task prompt_select_account_should_sign_in()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = IdentityServerPrincipal.Create("123", "dom"),
                PromptMode = OidcConstants.PromptModes.SelectAccount,
                Raw = new NameValueCollection()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task prompt_for_signin_should_remove_prompt_from_raw_url()
        {
            var request = new ValidatedAuthorizeRequest
            {
                ClientId = "foo",
                Subject = IdentityServerPrincipal.Create("123", "dom"),
                PromptMode = OidcConstants.PromptModes.Login,
                Raw = new NameValueCollection
                {
                    { OidcConstants.AuthorizeRequest.Prompt, OidcConstants.PromptModes.Login }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            request.Raw.AllKeys.Should().NotContain(OidcConstants.AuthorizeRequest.Prompt);
        }
    }
}
