// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;

namespace Voting.Basis.Controllers.Models;

public class GenerateExportRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the entity to export.
    /// The entity ID could be a political business (vote, proportional election, majority election) ID or a contest ID.
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }
}
