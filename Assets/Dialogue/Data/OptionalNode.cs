
namespace Dialogue.Data
{
    public class OptionNode : Node
    {
        [SerializeField] private List<Node> options { get; set; } = new List<Node>();

        public List<Node> Options => options;
    }
}