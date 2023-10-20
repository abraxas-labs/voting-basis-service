// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System;
using FluentAssertions;
using Voting.Basis.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class PublicKeySignatureDeleteHsmPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new PublicKeySignatureDeleteHsmPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-host",
            "my-key",
            MockedClock.GetDate(-3),
            10,
            new byte[] { 0x20, 0x30, 0xFF });
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LWhvc3RteS1rZXkAAAFvgCJQ2AAAAAAAAAAKvg66Lb52jbhuQlPbcZaKI3+up85tzkTmy1AqO5e5UHZG6c0KuRZ70o71I7ihGkd33hv4ktwDjGCwBMEfj7k2CA==");
    }
}
