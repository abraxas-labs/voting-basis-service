// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System;
using FluentAssertions;
using Voting.Basis.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class PublicKeySignatureDeleteAuthenticationTagPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new PublicKeySignatureDeleteAuthenticationTagPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-host",
            "my-key",
            MockedClock.GetDate(-3),
            10);
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LWhvc3RteS1rZXkAAAFvgCJQ2AAAAAAAAAAK");
    }
}