export default function (pluginManager) {
    
    // Function to handle the actual sending
    const sendToKindle = async (itemId) => {
        try {
            Dashboard.showLoadingMsg();
            const response = await ApiClient.fetch({
                type: 'POST',
                url: ApiClient.getUrl('SendToKindle/Send/' + itemId)
            });
            Dashboard.hideLoadingMsg();
            require(['toast'], function (toast) {
                toast("Successfully sent to Kindle!");
            });
        } catch (err) {
            Dashboard.hideLoadingMsg();
            require(['toast'], function (toast) {
                toast("Failed to send to Kindle. See logs.");
            });
        }
    };

    // Inject into Context Menu
    pluginManager.registerContextMenuItem({
        name: 'Send to Kindle',
        icon: 'menu_book',
        category: 'Play',
        visible: function(item) {
            return item.Type === 'Book';
        },
        onClick: function(item) {
            sendToKindle(item.Id);
        }
    });
}
