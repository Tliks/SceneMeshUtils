using nadena.dev.ndmf;
using nadena.dev.ndmf.preview;
using com.aoyon.scenemeshutils;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PluginDefinition))]

namespace com.aoyon.scenemeshutils
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "com.aoyon.scene-mesh-utils";

        public override string DisplayName => "SceneMeshUtils";

        protected override void Configure()
        {
            var sequence =
                InPhase(BuildPhase.Transforming)
                .BeforePlugin("MantisLODEditor.ndmf")
                .BeforePlugin("net.rs64.tex-trans-tool")
                .BeforePlugin("com.anatawa12.avatar-optimizer");

            sequence
            .Run(AddShrinkBlendShapePass.Instance)
            .PreviewingWith(new AddShrinkBlendShapePreview()).Then
            .Run(RemoveMeshFromScenePass.Instance)
            .PreviewingWith(new RemoveMeshFromScenePreview());
        }
    }
}