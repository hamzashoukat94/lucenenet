using System;
using System.Collections.Generic;
using Lucene.Net.Support;
using NUnit.Framework;
using System.Linq;

namespace Lucene.Net.Util
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

    [TestFixture]
    public class TestIdentityHashSet : LuceneTestCase
    {

        [Test]
        public virtual void TestCheck()
        {
            Random rnd = Random;

            ISet<object> jdk = Collections.NewSetFromMap<object, bool?>(new IdentityHashMap<object, bool?>());
            RamUsageEstimator.IdentityHashSet<object> us = new RamUsageEstimator.IdentityHashSet<object>();

            int max = 100000;
            int threshold = 256;
            for (int i = 0; i < max; i++)
            {
                // some of these will be interned and some will not so there will be collisions.
                int? v = rnd.Next(threshold);

                bool e1 = jdk.Contains(v);
                bool e2 = us.Contains(v);
                Assert.AreEqual(e1, e2);

                e1 = jdk.Add(v);
                e2 = us.Add(v);
                Assert.AreEqual(e1, e2);
            }

            ISet<object> collected = Collections.NewSetFromMap<object, bool?>(new IdentityHashMap<object, bool?>());
            foreach (object o in us)
            {
                collected.Add(o);
            }

            assertEquals(collected, jdk);
        }
    }
}