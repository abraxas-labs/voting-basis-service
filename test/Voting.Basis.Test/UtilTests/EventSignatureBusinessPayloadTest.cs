// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System;
using FluentAssertions;
using Voting.Basis.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Basis.Test.UtilTests;

public class EventSignatureBusinessPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new EventSignatureBusinessPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-stream",
            new byte[] { 0x10, 0x20, 0xFF },
            Guid.Parse("883d70e6-4218-422b-aab8-d2d17f2d4ecd"),
            "my-host",
            "my-key",
            MockedClock.GetDate(-10));
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LXN0cmVhbRAg/zg4M2Q3MGU2LTQyMTgtNDIyYi1hYWI4LWQyZDE3ZjJkNGVjZG15LWhvc3RteS1rZXkAAAFvXBXM2A==");
    }
}
