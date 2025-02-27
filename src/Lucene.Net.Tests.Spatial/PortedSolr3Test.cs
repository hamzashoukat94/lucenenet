﻿using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Vector;
using NUnit.Framework;
using RandomizedTesting.Generators;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Spatial
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Based off of Solr 3's SpatialFilterTest.
    /// </summary>
    public class PortedSolr3Test : StrategyTestCase
    {
        //@ParametersFactory
        public static IList<Object[]> Parameters()
        {
            IList<Object[]> ctorArgs = new JCG.List<object[]>();

            SpatialContext ctx = SpatialContext.GEO;
            SpatialPrefixTree grid;
            SpatialStrategy strategy;

            grid = new GeohashPrefixTree(ctx, 12);
            strategy = new RecursivePrefixTreeStrategy(grid, "recursive_geohash");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            grid = new QuadPrefixTree(ctx, 25);
            strategy = new RecursivePrefixTreeStrategy(grid, "recursive_quad");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            grid = new GeohashPrefixTree(ctx, 12);
            strategy = new TermQueryPrefixTreeStrategy(grid, "termquery_geohash");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            strategy = new PointVectorStrategy(ctx, "pointvector");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            return ctorArgs;
        }

        // this is a hack for clover! (otherwise strategy.toString() used as file name)
        public class Param
        {
            internal SpatialStrategy strategy;

            internal Param(SpatialStrategy strategy) { this.strategy = strategy; }


            public override String ToString() { return strategy.FieldName; }
        }

        //  private String fieldName;

        //public PortedSolr3Test(Param param)
        //{
        //    SpatialStrategy strategy = param.strategy;
        //    this.ctx = strategy.SpatialContext;
        //    this.strategy = strategy;
        //}

        public override void SetUp()
        {
            base.SetUp();
            SpatialStrategy strategy = ((Param)(RandomPicks.RandomFrom(Random, Parameters()))[0]).strategy;
            this.ctx = strategy.SpatialContext;
            this.strategy = strategy;
        }

        private void setupDocs()
        {
            base.DeleteAll();
            adoc("1", ctx.MakePoint(-79.9289094, 32.7693246));
            adoc("2", ctx.MakePoint(-80.9289094, 33.7693246));
            adoc("3", ctx.MakePoint(50.9289094, -32.7693246));
            adoc("4", ctx.MakePoint(60.9289094, -50.7693246));
            adoc("5", ctx.MakePoint(0, 0));
            adoc("6", ctx.MakePoint(0.1, 0.1));
            adoc("7", ctx.MakePoint(-0.1, -0.1));
            adoc("8", ctx.MakePoint(179.9, 0));
            adoc("9", ctx.MakePoint(-179.9, 0));
            adoc("10", ctx.MakePoint(50, 89.9));
            adoc("11", ctx.MakePoint(-130, 89.9));
            adoc("12", ctx.MakePoint(50, -89.9));
            adoc("13", ctx.MakePoint(-130, -89.9));
            Commit();
        }


        [Test]
        public virtual void TestIntersections()
        {
            setupDocs();
            //Try some edge cases
            //NOTE: 2nd arg is distance in kilometers
            CheckHitsCircle(ctx.MakePoint(1, 1), 175, 3, 5, 6, 7);
            CheckHitsCircle(ctx.MakePoint(179.8, 0), 200, 2, 8, 9);
            CheckHitsCircle(ctx.MakePoint(50, 89.8), 200, 2, 10, 11);//this goes over the north pole
            CheckHitsCircle(ctx.MakePoint(50, -89.8), 200, 2, 12, 13);//this goes over the south pole
                                                                      //try some normal cases
            CheckHitsCircle(ctx.MakePoint(-80.0, 33.0), 300, 2);
            //large distance
            CheckHitsCircle(ctx.MakePoint(1, 1), 5000, 3, 5, 6, 7);
            //Because we are generating a box based on the west/east longitudes and the south/north latitudes, which then
            //translates to a range query, which is slightly more inclusive.  Thus, even though 0.0 is 15.725 kms away,
            //it will be included, b/c of the box calculation.
            CheckHitsBBox(ctx.MakePoint(0.1, 0.1), 15, 2, 5, 6);
            //try some more
            DeleteAll();
            adoc("14", ctx.MakePoint(5, 0));
            adoc("15", ctx.MakePoint(15, 0));
            //3000KM from 0,0, see http://www.movable-type.co.uk/scripts/latlong.html
            adoc("16", ctx.MakePoint(19.79750, 18.71111));
            adoc("17", ctx.MakePoint(-95.436643, 44.043900));
            Commit();

            CheckHitsCircle(ctx.MakePoint(0, 0), 1000, 1, 14);
            CheckHitsCircle(ctx.MakePoint(0, 0), 2000, 2, 14, 15);
            CheckHitsBBox(ctx.MakePoint(0, 0), 3000, 3, 14, 15, 16);
            CheckHitsCircle(ctx.MakePoint(0, 0), 3001, 3, 14, 15, 16);
            CheckHitsCircle(ctx.MakePoint(0, 0), 3000.1, 3, 14, 15, 16);

            //really fine grained distance and reflects some of the vagaries of how we are calculating the box
            CheckHitsCircle(ctx.MakePoint(-96.789603, 43.517030), 109, 0);

            // falls outside of the real distance, but inside the bounding box
            CheckHitsCircle(ctx.MakePoint(-96.789603, 43.517030), 110, 0);
            CheckHitsBBox(ctx.MakePoint(-96.789603, 43.517030), 110, 1, 17);
        }

        //---- these are similar to Solr test methods

        private void CheckHitsCircle(IPoint pt, double distKM, int assertNumFound, params int[] assertIds)
        {
            _CheckHits(false, pt, distKM, assertNumFound, assertIds);
        }
        private void CheckHitsBBox(IPoint pt, double distKM, int assertNumFound, params int[] assertIds)
        {
            _CheckHits(true, pt, distKM, assertNumFound, assertIds);
        }

        private void _CheckHits(bool bbox, IPoint pt, double distKM, int assertNumFound, params int[] assertIds)
        {
            SpatialOperation op = SpatialOperation.Intersects;
            double distDEG = DistanceUtils.Dist2Degrees(distKM, DistanceUtils.EARTH_MEAN_RADIUS_KM);
            IShape shape = ctx.MakeCircle(pt, distDEG);
            if (bbox)
                shape = shape.BoundingBox;

            SpatialArgs args = new SpatialArgs(op, shape);
            //args.setDistPrecision(0.025);
            Query query;
            if (Random.nextBoolean())
            {
                query = strategy.MakeQuery(args);
            }
            else
            {
                query = new FilteredQuery(new MatchAllDocsQuery(), strategy.MakeFilter(args));
            }
            SearchResults results = executeQuery(query, 100);
            assertEquals("" + shape, assertNumFound, results.numFound);
            if (assertIds != null)
            {
                ISet<int> resultIds = new JCG.HashSet<int>();
                foreach (SearchResult result in results.results)
                {
                    resultIds.Add(int.Parse(result.document.Get("id"), CultureInfo.InvariantCulture));
                }
                foreach (int assertId in assertIds)
                {
                    assertTrue("has " + assertId, resultIds.Contains(assertId));
                }
            }
        }
    }
}
