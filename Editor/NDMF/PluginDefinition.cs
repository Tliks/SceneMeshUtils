using nadena.dev.ndmf;
using com.aoyon.scenemeshutils;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PluginDefinition))]

namespace com.aoyon.scenemeshutils
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "com.aoyon.scenemeshutils";

        public override string DisplayName => "SceneMeshUtils";

        protected override void Configure()
        {
            var sequence =
                InPhase(BuildPhase.Transforming)
                .BeforePlugin("net.rs64.tex-trans-tool")
                .BeforePlugin("com.anatawa12.avatar-optimizer");

            sequence.Run(AddShrinkBlendShapePass.Instance).Then
            .Run(RemoveMeshFromScenePass.Instance);
        }
    }
}