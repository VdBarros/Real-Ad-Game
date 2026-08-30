using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Presentation.Pure;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    public sealed class PartNamingTests
    {
        [Test]
        public void EveryPartStyleIsNamedUnderTheWorldPrefix()
        {
            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                Assert.That(
                    PartNames.IsWorldPrefixed(PartNames.WorldPrefix + style),
                    Is.True,
                    style + " must answer to the prefix the world materials are counted by");
            }
        }

        [Test]
        public void TheBackdropSkinDoesNotAnswerToTheWorldPrefix()
        {
            Assert.That(
                PartNames.IsWorldPrefixed(PartNames.BackdropSkin),
                Is.False,
                "the backdrop is one sheet behind the camera, not a part style, and a check that "
                + "counts world materials by prefix must not tally it as one");
        }

        [Test]
        public void NoOtherMintedNameAnswersToTheWorldPrefix()
        {
            foreach (var minted in Minted())
            {
                if (string.Equals(minted.Key, nameof(PartNames.WorldPrefix), StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(
                    PartNames.IsWorldPrefixed(minted.Value),
                    Is.False,
                    minted.Key + " (\"" + minted.Value + "\") collides with the world-material prefix, "
                    + "so anything counted by that prefix would tally it as a part style");
            }
        }

        [Test]
        public void NoNameDerivedFromAWorldMaterialAnswersToTheWorldPrefix()
        {
            foreach (var derivation in Derivations())
            {
                foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
                {
                    var worn = PartNames.WorldPrefix + style;
                    var derived = (string)derivation.Invoke(null, new object[] { worn });

                    Assert.That(
                        PartNames.IsWorldPrefixed(derived),
                        Is.False,
                        derivation.Name + " turns \"" + worn + "\" into \"" + derived
                        + "\", which still answers to the world-material prefix, so every check that "
                        + "counts world materials by prefix would tally the copy as a part style");
                }
            }
        }

        static IEnumerable<MethodInfo> Derivations()
        {
            var derivations = new List<MethodInfo>();

            foreach (var method in typeof(PartNames).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var taken = method.GetParameters();

                if (method.ReturnType == typeof(string)
                    && taken.Length == 1
                    && taken[0].ParameterType == typeof(string))
                {
                    derivations.Add(method);
                }
            }

            Assert.That(
                derivations.Count,
                Is.GreaterThan(0),
                "no name is minted out of another, so the naming authority went missing");

            return derivations;
        }

        static IEnumerable<KeyValuePair<string, string>> Minted()
        {
            var minted = new List<KeyValuePair<string, string>>();

            foreach (var field in typeof(PartNames).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.IsLiteral || field.FieldType != typeof(string))
                {
                    continue;
                }

                minted.Add(new KeyValuePair<string, string>(field.Name, (string)field.GetRawConstantValue()));
            }

            Assert.That(minted.Count, Is.GreaterThan(0), "the naming authority went missing");

            return minted;
        }
    }
}
