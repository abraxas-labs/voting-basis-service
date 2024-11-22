// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Basis.Data.Models;

namespace Voting.Basis.Ech.Models;

[Serializable]
public class BallotExtension
{
    [XmlElement(ElementName = "voteId", Namespace = "")]
    public Guid VoteId { get; set; }

    [XmlElement(ElementName = "voteType", Namespace = "")]
    public VoteType VoteType { get; set; }

    [XmlElement(ElementName = "voteOfficialDescription", Namespace = "")]
    public List<XmlKeyValuePair> VoteOfficicalDescription { get; set; } = new();

    [XmlElement(ElementName = "voteShortDescription", Namespace = "")]
    public List<XmlKeyValuePair> VoteShortDescription { get; set; } = new();

    [XmlElement(ElementName = "voteResultEntry", Namespace = "")]
    public VoteResultEntry VoteResultEntry { get; set; }

    [XmlElement(ElementName = "voteResultAlgorithm", Namespace = "")]
    public VoteResultAlgorithm VoteResultAlgorithm { get; set; }

    [XmlElement(ElementName = "voteReviewProcedure", Namespace = "")]
    public VoteReviewProcedure VoteReviewProcedure { get; set; }

    [XmlElement(ElementName = "voteEnforceResultEntryForCountingCircles", Namespace = "")]
    public bool VoteEnforceResultEntryForCountingCircles { get; set; }

    [XmlElement(ElementName = "ballotSubType", Namespace = "")]
    public BallotSubType? BallotSubType { get; set; }
}
