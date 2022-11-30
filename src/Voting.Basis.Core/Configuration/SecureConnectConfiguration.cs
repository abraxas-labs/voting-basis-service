// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Iam.AuthenticationScheme;

namespace Voting.Basis.Core.Configuration;

public class SecureConnectConfiguration : SecureConnectOptions
{
    public string ServiceUserId { get; set; } = string.Empty;

    public string AbraxasTenantId { get; set; } = string.Empty;
}
