(function () {
    'use strict';

    // Function to handle the actual sending
    const sendToKindle = async (itemId) => {
        try {
            Dashboard.showLoadingMsg();
            await ApiClient.fetch({
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

    // Use a MutationObserver to detect when Jellyfin opens a context menu / action sheet
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            mutation.addedNodes.forEach((node) => {
                if (node.nodeType === Node.ELEMENT_NODE) {
                    const actionSheet = node.querySelector('.actionSheet') || (node.classList && node.classList.contains('actionSheet') ? node : null);
                    
                    if (actionSheet) {
                        const menuScroller = actionSheet.querySelector('.actionSheetScroller') || actionSheet.querySelector('div');
                        
                        // Prevent duplicate injection
                        if (menuScroller && !menuScroller.querySelector('.custom-send-to-kindle')) {
                            
                            // Get the active item ID (usually stored on the focused card)
                            const activeCard = document.querySelector('.card.card-focused') || document.querySelector('.card.contextmenu-active');
                            const itemId = activeCard ? activeCard.getAttribute('data-id') : null;

                            if (itemId) {
                                // Create the custom menu button matching Jellyfin's native layout
                                const customButton = document.createElement('button');
                                customButton.className = 'listItem actionSheetMenuItem notFocusable custom-send-to-kindle';
                                customButton.innerHTML = `
                                    <div class="listItem-icon listItem-icon-transparent">
                                        <span class="material-icons actionSheetMenuItemIcon">menu_book</span>
                                    </div>
                                    <div class="listItemBody">
                                        <div class="listItemBodyText">Send to Kindle</div>
                                    </div>
                                `;

                                // Attach click event listener
                                customButton.addEventListener('click', () => {
                                    sendToKindle(itemId);
                                    
                                    // Close the action sheet programmatically after clicking
                                    const closeButton = actionSheet.querySelector('.btnCancel') || actionSheet.querySelector('button[data-id="cancel"]');
                                    if (closeButton) closeButton.click();
                                });

                                // Append to the action sheet options list
                                menuScroller.appendChild(customButton);
                            }
                        }
                    }
                }
            });
        });
    });

    // Start observing the document body for injected menus
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    console.log("SendToKindle Custom Context Menu Script Loaded Successfully.");
})();
