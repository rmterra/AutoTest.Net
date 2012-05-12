﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoTest.TestRunners.Shared.Options;
using System.Threading;
using AutoTest.TestRunners.Shared.Results;
using AutoTest.TestRunners.Shared.Targeting;
using AutoTest.TestRunners.Shared.AssemblyAnalysis;
using System.Diagnostics;

namespace AutoTest.TestRunners.Shared
{
    public class TestRunProcess : AutoTest.TestRunners.Shared.ITestRunProcess
    {
        private static List<TestResult> _results = new List<TestResult>();
        private IAssemblyPropertyReader _locator;
        private ITestRunProcessFeedback _feedback = null;
        private bool _runInParallel = false;
        private Func<bool> _abortWhen = null;
        private Action<AutoTest.TestRunners.Shared.Targeting.Platform, Version, Action<ProcessStartInfo, bool>> _processWrapper = null;
        private bool _compatabilityMode = false;

        public static void AddResults(IEnumerable<TestResult> results)
        {
            lock (_results)
            {
                _results.AddRange(results);
            }
        }

        public TestRunProcess()
        {
            _locator = new AssemblyPropertyReader();
        }

        public TestRunProcess(ITestRunProcessFeedback feedback)
        {
            _locator = new AssemblyPropertyReader();
            _feedback = feedback;
        }

        public TestRunProcess RunParallel()
        {
            _runInParallel = true;
            return this;
        }

        public TestRunProcess AbortWhen(Func<bool> abortWhen)
        {
            _abortWhen = abortWhen;
            return this;
        }

        public TestRunProcess WrapTestProcessWith(
			Action<
				AutoTest.TestRunners.Shared.Targeting.Platform,
				Version,
				Action<ProcessStartInfo, bool>> processWrapper)
		{
            _processWrapper = processWrapper;
			return this;
		}

        public TestRunProcess RunInCompatibilityMode()
        {
            _compatabilityMode = true;
            return this;
        }

		public TestSession Prepare(RunOptions options)
		{
			var processes = new List<TestProcess>();
            var testRuns = getTargetedRuns(options);
            foreach (var target in testRuns)
            {
                var process = new TestProcessLauncher(target, _feedback);
				if (_processWrapper != null)
                    process.WrapTestProcessWith(_processWrapper);
                if (_compatabilityMode)
                    process.RunInCompatibilityMode();
                process.AbortWhen(_abortWhen);
				processes.Add(process.Start());
            }
			return new TestSession(processes);
		}

        private IEnumerable<TargetedRun> getTargetedRuns(RunOptions options)
        {
            var assembler = new TargetedRunAssembler(options, _locator, _runInParallel);
            return assembler.Assemble();
        }
    }
}
