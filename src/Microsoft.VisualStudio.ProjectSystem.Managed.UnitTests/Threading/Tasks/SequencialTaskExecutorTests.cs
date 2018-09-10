﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Threading.Tasks
{

    [Trait("UnitTest", "ProjectSystem")]
    public class SequencialTaskExecutorTests
    {
        [Fact]
        public async Task EnsureTasksAreRunInOrder()
        {
            const int NumberOfTasks = 25;
            var sequencer = new SequencialTaskExecutor();

            var tasks = new List<Task>();
            var sequences = new List<int>();
            for (int i = 0; i < NumberOfTasks; i++)
            {
                int num = i;
                tasks.Add(sequencer.ExecuteTask(async () =>
                {
                    async Task func()
                    {
                        await Task.Delay(1);
                        sequences.Add(num);
                    }
                    await func();
                }));
            }

            await Task.WhenAll(tasks.ToArray());
            for (int i = 0; i < NumberOfTasks; i++)
            {
                Assert.Equal(i, sequences[i]);
            }
        }

        [Fact]
        public async Task EnsureNestedCallsAreExcecutedDirectly()
        {
            const int NumberOfTasks = 10;
            var sequencer = new SequencialTaskExecutor();

            var tasks = new List<Task>();
            var sequences = new List<int>();
            for (int i = 0; i < NumberOfTasks; i++)
            {
                int num = i;
                tasks.Add(sequencer.ExecuteTask(async () =>
                {
                    async Task func()
                    {
                        await sequencer.ExecuteTask(async () =>
                        {
                            await Task.Delay(1);
                            sequences.Add(num);
                        });
                    }
                    await func();
                }));
            }

            await Task.WhenAll(tasks.ToArray());
            for (int i = 0; i < NumberOfTasks; i++)
            {
                Assert.Equal(i, sequences[i]);
            }
        }

        [Fact]
        public void CalltoDisposedObjectShouldThrow()
        {
            var sequencer = new SequencialTaskExecutor();
            sequencer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { sequencer.ExecuteTask(() => Task.CompletedTask); });
        }

        [Fact]
        public async Task EnsureTasksCancelledWhenDisposed()
        {
            const int NumberOfTasks = 10;
            var sequencer = new SequencialTaskExecutor();

            var tasks = new List<Task>();
            for (int i = 0; i < NumberOfTasks; i++)
            {
                int num = i;
                tasks.Add(sequencer.ExecuteTask(async () =>
                {
                    async Task func()
                    {
                        await Task.Delay(100);
                    }
                    await func();
                }));
            }
            sequencer.Dispose();

            bool mustBeCancelled = false;

            try
            {
                await Task.WhenAll(tasks.ToArray());
            }
            catch (OperationCanceledException)
            {
                for (int i = 0; i < NumberOfTasks; i++)
                {
                    // The first task or two may already be running. So we skip completed tasks until we find 
                    // one that is is cancelled
                    if (mustBeCancelled)
                    {
                        Assert.True(tasks[i].IsCanceled);
                    }
                    else
                    {
                        // All remaining tasks should be cancelled
                        mustBeCancelled = tasks[i].IsCanceled;
                    }
                }
            }

            Assert.True(mustBeCancelled);
        }
    }
}
