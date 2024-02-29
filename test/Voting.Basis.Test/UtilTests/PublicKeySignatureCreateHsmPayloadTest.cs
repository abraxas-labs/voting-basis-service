// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Basis.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class PublicKeySignatureCreateHsmPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new PublicKeySignatureCreateHsmPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-host",
            "my-key",
            new byte[] { 0x20, 0x30, 0xFF },
            MockedClock.GetDate(-10),
            MockedClock.GetDate(-20),
            new byte[] { 0x10, 0x20, 0xFF });
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LWhvc3RteS1rZXm+DrotvnaNuG5CU9txloojf66nzm3ORObLUCo7l7lQdkbpzQq5FnvSjvUjuKEaR3feG/iS3AOMYLAEwR+PuTYIAAABb1wVzNgAAAFvKJY02DXjN3cFazplG0ETRxRfX96JHtEBYDIJmHtRe1uXreEaj8vpFKyc+vbP4T3XAJeVikRlEPCoItBNsVALCpthMRY=");
    }
}
