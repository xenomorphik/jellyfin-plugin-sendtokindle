(function () {
    'use strict';

    const getEmail = async () => {
        try {
            let res = await fetch(ApiClient.getUrl('SendToKindle/UserEmail'), {
                headers: {
                    'Authorization': 'MediaBrowser Token="' + ApiClient.accessToken() + '"',
                    'Content-Type': 'application/json'
                }
            });
            if (res.ok) {
                let data = await res.json();
                return data.Email;
            }
        } catch(e) { }
        return null;
    };

    const setEmail = async (email) => {
        await fetch(ApiClient.getUrl('SendToKindle/UserEmail'), {
            method: 'POST',
            headers: {
                'Authorization': 'MediaBrowser Token="' + ApiClient.accessToken() + '"',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ Email: email })
        });
    };

    const promptUserForEmail = async (currentEmail) => {
        return new Promise((resolve) => {
            let email = window.prompt("Enter your Target Kindle Email address (@kindle.com):", currentEmail || "");
            resolve(email);
        });
    };

    const sendToKindle = async (itemId) => {
        try {
            Dashboard.showLoadingMsg();
            let res = await fetch(ApiClient.getUrl('SendToKindle/Send/' + itemId), {
                method: 'POST',
                headers: {
                    'Authorization': 'MediaBrowser Token="' + ApiClient.accessToken() + '"',
                }
            });
            Dashboard.hideLoadingMsg();
            
            if (res.status === 412) {
                // Email not set
                let email = await promptUserForEmail("");
                if (email) {
                    await setEmail(email);
                    sendToKindle(itemId);
                }
                return;
            }
            if (!res.ok) throw new Error("Send failed");
            
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

    const openSettings = async () => {
        let current = await getEmail();
        let newEmail = await promptUserForEmail(current);
        if (newEmail !== null) {
            await setEmail(newEmail);
            require(['toast'], function(toast) { toast("Kindle email updated."); });
        }
    };

    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            mutation.addedNodes.forEach((node) => {
                if (node.nodeType === Node.ELEMENT_NODE) {
                    const actionSheet = node.querySelector('.actionSheet') || (node.classList && node.classList.contains('actionSheet') ? node : null);
                    
                    if (actionSheet) {
                        const menuScroller = actionSheet.querySelector('.actionSheetScroller') || actionSheet.querySelector('div');
                        
                        if (menuScroller && !menuScroller.querySelector('.custom-send-to-kindle')) {
                            
                            const activeCard = document.querySelector('.card.card-focused') || document.querySelector('.card.contextmenu-active');
                            const itemId = activeCard ? activeCard.getAttribute('data-id') : null;

                            if (itemId) {
                                const sendBtn = document.createElement('button');
                                sendBtn.className = 'listItem actionSheetMenuItem notFocusable custom-send-to-kindle';
                                sendBtn.innerHTML = `
                                    <div class="listItem-icon listItem-icon-transparent">
                                        <span class="material-icons actionSheetMenuItemIcon">menu_book</span>
                                    </div>
                                    <div class="listItemBody">
                                        <div class="listItemBodyText">Send to Kindle</div>
                                    </div>
                                `;
                                sendBtn.addEventListener('click', () => {
                                    sendToKindle(itemId);
                                    const closeBtn = actionSheet.querySelector('.btnCancel') || actionSheet.querySelector('button[data-id="cancel"]');
                                    if (closeBtn) closeBtn.click();
                                });
                                menuScroller.appendChild(sendBtn);

                                const settingsBtn = document.createElement('button');
                                settingsBtn.className = 'listItem actionSheetMenuItem notFocusable custom-send-to-kindle-settings';
                                settingsBtn.innerHTML = `
                                    <div class="listItem-icon listItem-icon-transparent">
                                        <span class="material-icons actionSheetMenuItemIcon">settings</span>
                                    </div>
                                    <div class="listItemBody">
                                        <div class="listItemBodyText">Kindle Settings</div>
                                    </div>
                                `;
                                settingsBtn.addEventListener('click', () => {
                                    openSettings();
                                    const closeBtn = actionSheet.querySelector('.btnCancel') || actionSheet.querySelector('button[data-id="cancel"]');
                                    if (closeBtn) closeBtn.click();
                                });
                                menuScroller.appendChild(settingsBtn);
                            }
                        }
                    }
                }
            });
        });
    });

    observer.observe(document.body, { childList: true, subtree: true });
    console.log("SendToKindle Custom Context Menu Script Loaded Successfully.");
})();
