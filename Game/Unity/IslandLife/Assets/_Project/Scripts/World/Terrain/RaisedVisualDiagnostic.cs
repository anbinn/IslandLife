using UnityEngine;

namespace IslandLife.World.Terrain
{
    /// <summary>The only diagnostic codes the Raised projection may ever emit.</summary>
    public static class RaisedVisualDiagnosticCodes
    {
        public const string MissingAuthorPrimitive = "MISSING_AUTHOR_PRIMITIVE";
        public const string MissingCanonicalTopology = "MISSING_CANONICAL_TOPOLOGY";
        public const string VisualConflict = "VISUAL_CONFLICT";
        public const string InvalidAssetReference = "INVALID_ASSET_REFERENCE";
    }

    /// <summary>
    /// A genuine implementation problem found while projecting Raised visuals.
    /// IL-WORLD-004S-R12.
    ///
    /// Before R12 an "unresolved" region meant "the shape of this whole connected region is not one we
    /// have proven". That conflated a coverage gap with a defect, and it made a red outline look like
    /// the user's data had been rejected. From R12 the Raised projection is per cell, so a freeform L,
    /// T, U, notch, ring, two-wide run or eight-by-four plateau is simply drawn, and the only things
    /// that can still be reported are real faults:
    ///
    ///   MISSING_AUTHOR_PRIMITIVE   - the composition set has no author slice for a needed role/slot
    ///   MISSING_CANONICAL_TOPOLOGY - the shared normalizer produced a state with no rule (a bug)
    ///   VISUAL_CONFLICT            - two logical cells claimed one visual coordinate (a bug)
    ///   INVALID_ASSET_REFERENCE    - the composition set itself is not fully assigned
    ///
    /// A shape category is deliberately NOT one of these. There is no NON_RECTANGULAR_REGION here any
    /// more, and no code path reports one.
    /// </summary>
    public readonly struct RaisedVisualDiagnostic
    {
        public RaisedVisualDiagnostic(string code, Vector3Int position, string message)
        {
            Code = code;
            Position = position;
            Message = message ?? string.Empty;
        }

        /// <summary>One of the four allowed codes; never a shape verdict.</summary>
        public string Code { get; }

        /// <summary>Where the problem is, so the editor can point at it.</summary>
        public Vector3Int Position { get; }

        public string Message { get; }

        /// <summary>True when the code is one of the four R12 permits.</summary>
        public bool IsKnownCode =>
            Code == RaisedVisualDiagnosticCodes.MissingAuthorPrimitive
            || Code == RaisedVisualDiagnosticCodes.MissingCanonicalTopology
            || Code == RaisedVisualDiagnosticCodes.VisualConflict
            || Code == RaisedVisualDiagnosticCodes.InvalidAssetReference;

        public override string ToString()
        {
            return $"{Code} at ({Position.x},{Position.y}) : {Message}";
        }
    }
}
