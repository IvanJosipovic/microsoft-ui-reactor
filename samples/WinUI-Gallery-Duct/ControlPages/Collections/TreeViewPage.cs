using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class TreeViewPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(16,
                PageHeader("TreeView", "A hierarchical list with expanding and collapsing nodes."),

                SampleCard("Basic TreeView",
                    TreeView(
                        TreeNode("Documents",
                            TreeNode("Work",
                                TreeNode("Report.docx"),
                                TreeNode("Slides.pptx")),
                            TreeNode("Personal",
                                TreeNode("Budget.xlsx"))),
                        TreeNode("Pictures",
                            TreeNode("Vacation",
                                TreeNode("Beach.jpg"),
                                TreeNode("Mountain.jpg")),
                            TreeNode("Family")),
                        TreeNode("Music")
                    ).Height(300),
                    @"TreeView(\n    TreeNode(""Documents"",\n        TreeNode(""Work"",\n            TreeNode(""Report.docx""),\n            TreeNode(""Slides.pptx""))),\n    TreeNode(""Pictures"", ...)\n)"),

                SampleCard("Deeply Nested TreeView",
                    TreeView(
                        TreeNode("Root",
                            TreeNode("Level 1A",
                                TreeNode("Level 2A",
                                    TreeNode("Level 3A"),
                                    TreeNode("Level 3B")),
                                TreeNode("Level 2B")),
                            TreeNode("Level 1B",
                                TreeNode("Level 2C")))
                    ).Height(250),
                    @"TreeView(\n    TreeNode(""Root"",\n        TreeNode(""Level 1A"",\n            TreeNode(""Level 2A"",\n                TreeNode(""Level 3A""))))\n)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
