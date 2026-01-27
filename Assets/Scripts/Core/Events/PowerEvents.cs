using System.Collections.Generic;
using UnityEngine;

namespace CarbonWorld.Core.Events
{
    public struct PowerStateChanged : IEvent
    {
        public HashSet<Vector3Int> AffectedPositions;
    }
}
