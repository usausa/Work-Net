namespace WorkChat.Selectors;

using WorkChat.Models;

public sealed class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate SendTemplate { get; set; } = default!;

    public DataTemplate ReceiveTemplate { get; set; } = default!;

    public DataTemplate SystemTemplate { get; set; } = default!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is not ChatMessage message)
        {
            return SystemTemplate;
        }

        return message.Type switch
        {
            MessageType.Send => SendTemplate,
            MessageType.Receive => ReceiveTemplate,
            _ => SystemTemplate
        };
    }
}
