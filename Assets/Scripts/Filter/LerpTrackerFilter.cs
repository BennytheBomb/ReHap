using System.Collections.Generic;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

namespace QuestMarkerTracking.Filter
{
    public class LerpTrackerFilter : AbstractTrackerFilter
    {
        [SerializeField] [Range(0, 1)] private float smoothing = 0.05f;
        [SerializeField] private float lagCompensation = 0.05f;

        private readonly Dictionary<int, PoseData> _trackers = new();

        public override PoseData UpdateTracker(int id, PoseData pose, float deltaTimestampSeconds)
        {
            _trackers.TryAdd(id, pose);
            var tracker = _trackers[id];

            var filteredPosition = Vector3.Lerp(pose.pos, tracker.pos, smoothing);
            var filteredRotation = Quaternion.Slerp(pose.rot, tracker.rot, smoothing);

            tracker.pos = filteredPosition;
            tracker.rot = filteredRotation;

            var velocity = filteredPosition - tracker.pos;

            return new PoseData
            {
                pos = filteredPosition + velocity * lagCompensation,
                rot = filteredRotation
            };
        }
    }
}