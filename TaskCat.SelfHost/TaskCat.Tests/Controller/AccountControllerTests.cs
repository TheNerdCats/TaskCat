﻿namespace TaskCat.Tests.Controller
{
    using Moq;
    using NUnit.Framework;
    using Data.Entity.Identity;
    using Data.Model.Identity.Registration;
    using Data.Model.Identity.Profile;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http.Results;
    using Common.Email;
    using TaskCat.Account.Core;
    using TaskCat.Account.Controllers;
    using AppSettings = Its.Configuration.Settings;
    using Common.Settings;

    [TestFixture]
    public class AccountControllerTests
    {

        [Test]
        public async Task Test_ResendConfirmationEmail_With_SucessfulResult()
        {
            AppSettings.Set<ClientSettings>(new ClientSettings()
            {
                AuthenticationIssuerName = "TaskCat.Auth",
                ConfirmEmailPath = "confirmEmail",
                HostingAddress = "TestHostAddress",
                WebCatUrl = "WebCatUrl"
            });

            Mock<IAccountContext> accountContextMock = new Mock<IAccountContext>();
            accountContextMock.Setup(x => x.FindUser(It.IsAny<string>())).ReturnsAsync(
                new User(new UserRegistrationModel(), new UserProfile()));
            accountContextMock.Setup(x => x.NotifyUserCreationByMail(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SendEmailResponse(HttpStatusCode.OK, null));

            AccountController accountController = new AccountController(accountContextMock.Object);
            var result = await accountController.ResendConfirmationEmail("123");

            Assert.IsInstanceOf<OkNegotiatedContentResult<SendEmailResponse>>(result);
            Assert.IsNotNull(result);

            var convertedResult = result as OkNegotiatedContentResult<SendEmailResponse>;
            Assert.IsNotNull(convertedResult.Content);

            Assert.IsTrue(convertedResult.Content.Success);
            Assert.AreEqual(HttpStatusCode.OK, convertedResult.Content.StatusCode);
        }

        [Test]
        public async Task Test_ResendConfirmationEmail_With_FailedResult()
        {
            AppSettings.Set<ClientSettings>(new ClientSettings()
            {
                AuthenticationIssuerName = "TaskCat.Auth",
                ConfirmEmailPath = "confirmEmail",
                HostingAddress = "TestHostAddress",
                WebCatUrl = "WebCatUrl"
            });

            Mock<IAccountContext> accountContextMock = new Mock<IAccountContext>();
            accountContextMock.Setup(x => x.FindUser(It.IsAny<string>())).ReturnsAsync(
                new User(new UserRegistrationModel(), new UserProfile()));
            accountContextMock.Setup(x => x.NotifyUserCreationByMail(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SendEmailResponse(HttpStatusCode.InternalServerError, "Random mail error"));

            AccountController accountController = new AccountController(accountContextMock.Object);
            var result = await accountController.ResendConfirmationEmail("123");

            Assert.IsInstanceOf<FormattedContentResult<SendEmailResponse>>(result);
            Assert.IsNotNull(result);

            FormattedContentResult<SendEmailResponse> convertedResult = result as FormattedContentResult<SendEmailResponse>;
            Assert.IsNotNull(convertedResult.Content);

            Assert.IsFalse(convertedResult.Content.Success);
            Assert.AreEqual(HttpStatusCode.InternalServerError, convertedResult.Content.StatusCode);
        }
    }
}
