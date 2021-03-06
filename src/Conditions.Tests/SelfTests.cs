﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Conditions.Tests
{
    [TestClass]
    public class SelfTests
    {
        private enum DescriptionStatus
        {
            Correct = 0,
            NoDescriptionAttribute,
            NoDescriptionMessage,
            IncorrectDescriptionMessage,
        }

        [TestMethod]
        [Description("This test tests the tests and searches for methods that lack the [TestMethod] attribute.")]
        public void AllPublicMethodsShouldBeMarkedWithTheTestAttribute()
        {
            IEnumerable<Type> unitTestClasses =
                from t in typeof (TestHelper).Assembly.GetTypes()
                where t.GetCustomAttributes(typeof (TestClassAttribute), true).Length > 0
                select t;

            MethodBase[] untestableMethods =
                (from unitTestClass in unitTestClasses
                    from publicMethod in unitTestClass.GetMethods()
                    where publicMethod.GetParameters().Length == 0
                    where publicMethod.GetCustomAttributes(typeof (TestMethodAttribute), true).Length == 0
                    where publicMethod.ReturnType == typeof (void)
                    select publicMethod).ToArray();

            if (untestableMethods.Length == 0)
            {
                return;
            }

            string message = "The following public methods that aren't marked with the " +
                             "[TestMethod] attribute:" + Environment.NewLine;

            foreach (var method in untestableMethods)
            {
                message += method.Name + Environment.NewLine;
            }

            Assert.Fail(message);
        }

        [TestMethod]
        [Description("This test tests the tests and searches for test methods that lack a proper description.")]
        public void AllTestsShouldHaveAValidDescription()
        {
            IEnumerable<Type> unitTestClasses =
                from t in typeof (TestHelper).Assembly.GetTypes()
                where t.GetCustomAttributes(typeof (TestClassAttribute), true).Length > 0
                select t;

            MethodBase[] testMethods =
                (from unitTestClass in unitTestClasses
                    from testMethod in unitTestClass.GetMethods()
                    where testMethod.GetParameters().Length == 0
                    where testMethod.GetCustomAttributes(typeof (TestMethodAttribute), true).Length == 1
                    where testMethod.ReturnType == typeof (void)
                    where GetMethodDescriptionCheck(testMethod) != DescriptionStatus.Correct
                    select testMethod).ToArray();

            if (testMethods.Length == 0)
            {
                return;
            }

            string message = "The following test methods have an incorrect [Description(..)] message. " +
                             "The description should not be empty and should end with 'should fail.' when an exception " +
                             "is expected." + Environment.NewLine;

            foreach (var method in testMethods)
            {
                message += method.DeclaringType.Name + "." + method.Name + " reason: " +
                           GetMethodDescriptionCheck(method).ToString() + Environment.NewLine;
            }

            Assert.Fail(message);
        }

        private static DescriptionStatus GetMethodDescriptionCheck(MethodBase testMethod)
        {
            var descriptions =
                testMethod.GetCustomAttributes(typeof (DescriptionAttribute), true)
                    .Cast<DescriptionAttribute>().ToArray();

            if (descriptions == null || descriptions.Length == 0)
            {
                return DescriptionStatus.NoDescriptionAttribute;
            }

            string description = descriptions[0].Description;

            if (String.IsNullOrEmpty(description))
            {
                return DescriptionStatus.NoDescriptionMessage;
            }

            var methodShouldThrowException =
                testMethod.GetCustomAttributes(typeof (ExpectedExceptionAttribute), true).Length > 0;

            if (methodShouldThrowException && !description.Contains("fail") && !description.Contains("throw"))
            {
                return DescriptionStatus.IncorrectDescriptionMessage;
            }

            return DescriptionStatus.Correct;
        }
    }
}