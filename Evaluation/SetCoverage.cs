﻿namespace prefSQL.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class SetCoverage
    {
        /// <summary>
        ///     Determines the percentage of objects in normalizedDataToBeCovered covered by objects in
        ///     normalizedDataCoveringDataToBeCovered according to W.-T. Balke, J. X. Zheng, and U. Güntzer (2005).
        /// </summary>
        /// <remarks>
        ///     Publication:
        ///     W.-T. Balke, J. X. Zheng, and U. Güntzer, “Approaching the Efficient Frontier: Cooperative Database Retrieval Using
        ///     High-Dimensional Skylines,” in Lecture Notes in Computer Science, Database Systems for Advanced Applications, D.
        ///     Hutchison, T. Kanade, J. Kittler, J. M. Kleinberg, F. Mattern, J. C. Mitchell, M. Naor, O. Nierstrasz, C. Pandu
        ///     Rangan, B. Steffen, M. Sudan, D. Terzopoulos, D. Tygar, M. Y. Vardi, G. Weikum, L. Zhou, B. C. Ooi, and X. Meng,
        ///     Eds, Berlin, Heidelberg: Springer Berlin Heidelberg, 2005, pp. 410–421.
        /// </remarks>
        /// <param name="normalizedDataToBeCovered">
        ///     The set of objects which should be covered by normalizedDataCoveringDataToBeCovered.
        /// </param>
        /// <param name="normalizedDataCoveringDataToBeCovered">The set of objects which should cover normalizedDataToBeCovered.</param>
        /// <param name="useColumns">
        ///     Array indices which should be used for the distance calculation. Useful if the object consists
        ///     of more dimensions than those over which the distance should be calculated, e.g. if the objects are database
        ///     objects with additional columns.
        /// </param>
        /// <returns>
        ///     The percentage of objects in normalizedDataToBeCovered covered by objects in
        ///     normalizedDataCoveringDataToBeCovered, i.e., the percentage of objects from normalizedDataCoveringDataToBeCovered
        ///     assigned to objects in normalizedDataToBeCovered. 0 = no objects covered, 1 = all objects covered.
        /// </returns>
        public static double GetCoverage(IReadOnlyDictionary<long, object[]> normalizedDataToBeCovered,
            IReadOnlyDictionary<long, object[]> normalizedDataCoveringDataToBeCovered, int[] useColumns)
        {
            var keysOfCoveredObjects = new HashSet<long>();

            // for each object in normalizedDataCoveringDataToBeCovered, find its corresponding, nearest (i.e., covered) object
            foreach (KeyValuePair<long, object[]> coveringObject in normalizedDataCoveringDataToBeCovered)
            {
                long coveredObjectKey = GetCoveredObject(coveringObject.Value, normalizedDataToBeCovered,
                    useColumns).Item1;
                keysOfCoveredObjects.Add(coveredObjectKey);
            }

            return (double) keysOfCoveredObjects.Count / normalizedDataToBeCovered.Count;
        }

        /// <summary>
        /// TODO: rephrase since all distances returned
        ///     Calculates the representation error of normalizedDataCoveringDataToBeCovered according to Y. Tao, L. Ding, X. Lin,
        ///     and J. Pei (2009).
        /// </summary>
        /// <remarks>
        ///     The representation error is defined as the maximum of the minimum distances between each object in
        ///     normalizedDataToBeCovered and their covering object in normalizedDataCoveringDataToBeCovered.
        ///     Publication:
        ///     Y. Tao, L. Ding, X. Lin, and J. Pei, “Distance-Based Representative Skyline,” in 2009 IEEE 25th International
        ///     Conference on Data Engineering (ICDE), 2009, pp. 892–903.
        /// </remarks>
        /// <param name="normalizedDataToBeCovered">
        ///     The set of objects which should be covered by normalizedDataCoveringDataToBeCovered.
        /// </param>
        /// <param name="normalizedDataCoveringDataToBeCovered">The set of objects which should cover normalizedDataToBeCovered.</param>
        /// <param name="useColumns">
        ///     Array indices which should be used for the distance calculation. Useful if the object consists
        ///     of more dimensions than those over which the distance should be calculated, e.g. if the objects are database
        ///     objects with additional columns.
        /// </param>
        /// <returns>
        ///     The representation error of normalizedDataCoveringDataToBeCovered, i.e., the maximum of the minimum distances
        ///     between each object in normalizedDataToBeCovered and their covering object in
        ///     normalizedDataCoveringDataToBeCovered.
        /// </returns>
        public static Dictionary<long, double>.ValueCollection GetRepresentationError(
            IReadOnlyDictionary<long, object[]> normalizedDataToBeCovered,
            IReadOnlyDictionary<long, object[]> normalizedDataCoveringDataToBeCovered, int[] useColumns)
        {
            var distances = new Dictionary<long, double>();

            // for each object in normalizedDataToBeCovered, find its corresponding, nearest (i.e., covering) object
            foreach (KeyValuePair<long, object[]> objectToBeCovered in normalizedDataToBeCovered)
            {
                double distance = GetCoveredObject(objectToBeCovered.Value, normalizedDataCoveringDataToBeCovered,
                    useColumns).Item2;

                distances.Add(objectToBeCovered.Key, distance);
            }

            return distances.Values;
        }

        /// <summary>
        ///     Determines the object closest to the coveringObject, i.e., the object in normalizedDataToBeCovered which is
        ///     "covered" by the coveringObject, making coveringObject its nearest representative.
        /// </summary>
        /// <param name="coveringObject">
        ///     An object covering another object in normalizedDataToBeCovered. The latter object is
        ///     covered by the former if its distance to coveringObject is minimal.
        /// </param>
        /// <param name="normalizedDataToBeCovered">
        ///     The set examined for coverage. The coveringObject covers the object in
        ///     normalizedDataToBeCovered when its distance to coveringObject is minimal.
        /// </param>
        /// <param name="useColumns">Array indices which should be used for the distance calculation.</param>
        /// <returns>A Tuple with the key of the covered object in Item1 and its distance to coveringObject in Item2.</returns>
        internal static Tuple<long, double> GetCoveredObject(object[] coveringObject,
            IReadOnlyDictionary<long, object[]> normalizedDataToBeCovered, int[] useColumns)
        {
            long minimumDistanceObjectKey = -1;
            double minimumDistance = double.MaxValue;

            foreach (KeyValuePair<long, object[]> potentiallyCoveredObject in normalizedDataToBeCovered)
            {
                double euclideanDistance = CalculateEuclideanDistance(coveringObject, potentiallyCoveredObject.Value,
                    useColumns);

                if (euclideanDistance < minimumDistance)
                {
                    minimumDistance = euclideanDistance;
                    minimumDistanceObjectKey = potentiallyCoveredObject.Key;
                }
            }

            return new Tuple<long, double>(minimumDistanceObjectKey, minimumDistance);
        }

        /// <summary>
        ///     Calculates the Euclidean Distance between two objects with respect to the dimensions specified in useColumns.
        /// </summary>
        /// <param name="item1">First object used for distance calculation.</param>
        /// <param name="item2">Second object used for distance calculation.</param>
        /// <param name="useColumns">
        ///     Array indices which should be used for the distance calculation.
        /// </param>
        /// <returns></returns>
        internal static double CalculateEuclideanDistance(object[] item1, object[] item2, int[] useColumns)
        {
            double sum = 0;

            foreach (int useColumnIndex in useColumns)
            {
                double distance = (double) item1[useColumnIndex] - (double) item2[useColumnIndex];
                sum += distance * distance;
            }

            return Math.Sqrt(sum);
        }
    }
}