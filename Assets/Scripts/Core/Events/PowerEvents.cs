using System.Collections.Generic;
using UnityEngine;

namespace AncientFactory.Core.Events
{
    public struct PowerStateChanged : IEvent
    {
        public HashSet<Vector3Int> AffectedPositions;
    }
}
