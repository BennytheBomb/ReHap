using System.Collections.Generic;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.VideoModule;
using UnityEngine;

namespace QuestMarkerTracking.Filter
{
    public class KalmanTrackerFilter : AbstractTrackerFilter
    {
        private const int STATE_SIZE = 18; // pos, pos_vel, pos_acc, rotVec, rotVec_vel, rotVec_acc (3 each)
        private const int MEASUREMENT_SIZE = 6; // pos, rotVec (3 each)
        private const int CONTROL_SIZE = 0;

        [SerializeField] private double processNoise = 1e-4;
        [SerializeField] private double measurementNoise = 1e-5;
        [SerializeField] private double errorCovPost = 0.1;

        private readonly Dictionary<int, KalmanTracker> _kalmanTrackers = new();

        public override PoseData UpdateTracker(int id, PoseData pose, float deltaTimestampSeconds)
        {
            if (!_kalmanTrackers.ContainsKey(id))
            {
                _kalmanTrackers[id] = new KalmanTracker(processNoise, measurementNoise, errorCovPost);
            }

            var tracker = _kalmanTrackers[id];
            return tracker.EstimateTracker(pose, deltaTimestampSeconds);
        }

        private class KalmanTracker
        {
            private readonly KalmanFilter _kalmanFilter;
            private readonly Mat _measurement;

            public KalmanTracker(double processNoise, double measurementNoise, double errorCovPost)
            {
                _kalmanFilter = new KalmanFilter(STATE_SIZE, MEASUREMENT_SIZE, CONTROL_SIZE, CvType.CV_32FC1);

                var transitionMatrix = Mat.eye(STATE_SIZE, STATE_SIZE, CvType.CV_32F);
                _kalmanFilter.set_transitionMatrix(transitionMatrix);

                _measurement = Mat.zeros(MEASUREMENT_SIZE, 1, CvType.CV_32FC1);

                var statePreMat = Mat.zeros(STATE_SIZE, 1, CvType.CV_32FC1);
                _kalmanFilter.set_statePre(statePreMat);

                var statePostMat = Mat.zeros(STATE_SIZE, 1, CvType.CV_32FC1);
                _kalmanFilter.set_statePost(statePostMat);

                var measurementMat = Mat.zeros(MEASUREMENT_SIZE, STATE_SIZE, CvType.CV_32FC1);
                measurementMat.put(0, 0, 1); // x
                measurementMat.put(1, 1, 1); // y
                measurementMat.put(2, 2, 1); // z
                measurementMat.put(3, 9, 1); // rx
                measurementMat.put(4, 10, 1); // ry
                measurementMat.put(5, 11, 1); // rz
                _kalmanFilter.set_measurementMatrix(measurementMat);

                var processNoiseCovMat = Mat.eye(STATE_SIZE, STATE_SIZE, CvType.CV_32FC1) * processNoise;
                _kalmanFilter.set_processNoiseCov(processNoiseCovMat);

                var measurementNoiseCovMat = Mat.eye(MEASUREMENT_SIZE, MEASUREMENT_SIZE, CvType.CV_32FC1) * measurementNoise;
                _kalmanFilter.set_measurementNoiseCov(measurementNoiseCovMat);

                var errorCovPostMat = Mat.eye(STATE_SIZE, STATE_SIZE, CvType.CV_32FC1) * errorCovPost;
                _kalmanFilter.set_errorCovPost(errorCovPostMat);
            }

            private void UpdateTransitionMatrix(float dt)
            {
                var transitionMatrix = Mat.eye(STATE_SIZE, STATE_SIZE, CvType.CV_32FC1);

                transitionMatrix.put(0, 3, dt);
                transitionMatrix.put(1, 4, dt);
                transitionMatrix.put(2, 5, dt);
                transitionMatrix.put(3, 6, dt);
                transitionMatrix.put(4, 7, dt);
                transitionMatrix.put(5, 8, dt);
                transitionMatrix.put(0, 6, 0.5 * dt * dt);
                transitionMatrix.put(1, 7, 0.5 * dt * dt);
                transitionMatrix.put(2, 8, 0.5 * dt * dt);

                transitionMatrix.put(9, 12, dt);
                transitionMatrix.put(10, 13, dt);
                transitionMatrix.put(11, 14, dt);
                transitionMatrix.put(12, 15, dt);
                transitionMatrix.put(13, 16, dt);
                transitionMatrix.put(14, 17, dt);
                transitionMatrix.put(9, 15, 0.5 * dt * dt);
                transitionMatrix.put(10, 16, 0.5 * dt * dt);
                transitionMatrix.put(11, 17, 0.5 * dt * dt);

                _kalmanFilter.set_transitionMatrix(transitionMatrix);
            }

            public PoseData EstimateTracker(PoseData rawPose, float dt)
            {
                UpdateTransitionMatrix(dt);

                _kalmanFilter.predict();

                // Convert raw quaternion to rotation vector
                var q = rawPose.rot;
                var m = Matrix4x4.Rotate(q);
                var rotMat = new Mat(3, 3, CvType.CV_32F);
                for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                {
                    rotMat.put(i, j, m[i, j]);
                }

                var rvec = new Mat();
                Calib3d.Rodrigues(rotMat, rvec);

                _measurement.put(0, 0, new[]
                {
                    rawPose.pos.x,
                    rawPose.pos.y,
                    rawPose.pos.z,
                    (float)rvec.get(0, 0)[0],
                    (float)rvec.get(1, 0)[0],
                    (float)rvec.get(2, 0)[0]
                });

                using var correctedState = _kalmanFilter.correct(_measurement);

                var pos = new Vector3((float)correctedState.get(0, 0)[0],
                    (float)correctedState.get(1, 0)[0],
                    (float)correctedState.get(2, 0)[0]);

                var vel = new Vector3((float)correctedState.get(3, 0)[0],
                    (float)correctedState.get(4, 0)[0],
                    (float)correctedState.get(5, 0)[0]);

                var acc = new Vector3((float)correctedState.get(6, 0)[0],
                    (float)correctedState.get(7, 0)[0],
                    (float)correctedState.get(8, 0)[0]);

                var rvecFiltered = new Mat(3, 1, CvType.CV_32F);
                rvecFiltered.put(0, 0, correctedState.get(9, 0)[0]);
                rvecFiltered.put(1, 0, correctedState.get(10, 0)[0]);
                rvecFiltered.put(2, 0, correctedState.get(11, 0)[0]);

                var rotVel = new Vector3((float)correctedState.get(12, 0)[0],
                    (float)correctedState.get(13, 0)[0],
                    (float)correctedState.get(14, 0)[0]);

                var rotAcc = new Vector3((float)correctedState.get(15, 0)[0],
                    (float)correctedState.get(16, 0)[0],
                    (float)correctedState.get(17, 0)[0]);

                var predictedPos = pos + vel * dt + acc * (0.5f * dt * dt);
                var predictedRotVec = new Vector3((float)(rvecFiltered.get(0, 0)[0] + rotVel.x * dt + 0.5f * rotAcc.x * dt * dt),
                    (float)(rvecFiltered.get(1, 0)[0] + rotVel.y * dt + 0.5f * rotAcc.y * dt * dt),
                    (float)(rvecFiltered.get(2, 0)[0] + rotVel.z * dt + 0.5f * rotAcc.z * dt * dt));

                var predictedRvec = new Mat(3, 1, CvType.CV_32F);
                predictedRvec.put(0, 0, predictedRotVec.x);
                predictedRvec.put(1, 0, predictedRotVec.y);
                predictedRvec.put(2, 0, predictedRotVec.z);

                var predictedRmat = new Mat();
                Calib3d.Rodrigues(predictedRvec, predictedRmat);
                var predictedUnityMatrix = Matrix4x4.identity;
                for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                {
                    predictedUnityMatrix[i, j] = (float)predictedRmat.get(i, j)[0];
                }

                var predictedRotation = predictedUnityMatrix.rotation;

                return new PoseData
                {
                    pos = predictedPos,
                    rot = predictedRotation
                };
            }
        }
    }
}