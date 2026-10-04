(function () {
    'use strict';

    let lastClickedItemId = null;

    // Track the last clicked card or item to get its ID
    document.addEventListener('contextmenu', function(e) {
        let card = e.target.closest('.card') || e.target.closest('[data-id]');
        if (card) {
            lastClickedItemId = card.getAttribute('data-id');
        }
    }, true);

    document.addEventListener('click', function(e) {
        let btn = e.target.closest('[data-action="menu"]') || e.target.closest('.btnItemMenu');
        if (btn) {
            let card = btn.closest('.card') || btn.closest('[data-id]');
            if (card) {
                lastClickedItemId = card.getAttribute('data-id');
            }
        }
    }, true);

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
            if (!res.ok) {
                let errorText = "Send failed";
                try { 
                    errorText = await res.text(); 
                    if (!errorText) errorText = "Send failed";
                } catch(e) {}
                throw new Error(errorText);
            }
            
            require(['toast'], function (toast) {
                toast("Delivery Queued! Your book will be sent to your Kindle shortly.");
            });
        } catch (err) {
            Dashboard.hideLoadingMsg();
            require(['toast'], function (toast) {
                toast(err.message === "Send failed" ? "Failed to send to Kindle. See logs." : err.message);
            });
        }
    };

    function tryInjectProfileSettings() {
        if (!window.location.hash.includes('settings/profile')) return;
        
        let container = document.querySelector('.userSettingsPage .formContainer form') 
                     || document.querySelector('.userSettingsPage form') 
                     || (document.querySelector('.page:not(.hide)') ? document.querySelector('.page:not(.hide)').querySelector('form') : null);
                     
        if (container && !document.getElementById('sendToKindleProfileSection')) {
            let section = document.createElement('div');
            section.id = 'sendToKindleProfileSection';
            section.className = 'detailSection';
            section.style.marginTop = '2em';
            section.style.marginBottom = '2em';
            section.innerHTML = `
                <div class="detailSectionHeader">
                    <h2 class="detailSectionTitle">Send to Kindle</h2>
                </div>
                <div class="inputContainer">
                    <label class="inputLabel" for="txtKindleEmailProfile">Target Kindle Email</label>
                    <input is="emby-input" type="email" id="txtKindleEmailProfile" class="emby-input" placeholder="youremail@kindle.com" style="width: 100%; max-width: 400px; padding: .5em; border: 1px solid #555; border-radius: 4px; background: rgba(0,0,0,0.2); color: inherit;" />
                    <div class="fieldDescription">The email address of your Kindle device. (Must be authorized in your Amazon account)</div>
                </div>
                <button is="emby-button" type="button" class="raised button-submit block emby-button" id="btnSaveKindleEmailProfile" style="margin-top: 1em; padding: 0.5em 1em; background-color: #00a4dc; color: white; border: none; border-radius: 4px; cursor: pointer;">
                    <span>Save Kindle Email</span>
                </button>
            `;
            
            const saveBtnContainer = container.querySelector('.formSubmitContainer') || container.querySelector('button[type="submit"]')?.parentNode;
            if (saveBtnContainer) {
                container.insertBefore(section, saveBtnContainer);
            } else {
                container.appendChild(section);
            }

            getEmail().then(email => {
                if (email) document.getElementById('txtKindleEmailProfile').value = email;
            });

            document.getElementById('btnSaveKindleEmailProfile').addEventListener('click', async (e) => {
                e.preventDefault();
                const email = document.getElementById('txtKindleEmailProfile').value;
                await setEmail(email);
                require(['toast'], function (toast) { toast("Kindle email saved successfully!"); });
            });
        }
    }

    const observer = new MutationObserver((mutations) => {
        let shouldCheckProfile = false;
        
        mutations.forEach((mutation) => {
            mutation.addedNodes.forEach((node) => {
                if (node.nodeType === Node.ELEMENT_NODE) {
                    shouldCheckProfile = true;

                    const actionSheet = node.querySelector('.actionSheet') || (node.classList && node.classList.contains('actionSheet') ? node : null);
                    
                    if (actionSheet) {
                        const menuScroller = actionSheet.querySelector('.actionSheetScroller') || actionSheet.querySelector('div');
                        
                        if (menuScroller && !menuScroller.querySelector('.custom-send-to-kindle')) {
                            
                            let itemId = null;
                            const urlParams = new URLSearchParams(window.location.search || window.location.hash.split('?')[1]);
                            if (urlParams.has('id')) {
                                itemId = urlParams.get('id');
                            }
                            
                            if (!itemId) {
                                itemId = lastClickedItemId;
                            }

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
                            }
                        }
                    }
                }
            });
        });

        if (shouldCheckProfile) {
            tryInjectProfileSettings();
        }
    });

    observer.observe(document.body, { childList: true, subtree: true });
    
    window.addEventListener('hashchange', () => {
        setTimeout(tryInjectProfileSettings, 100);
        setTimeout(tryInjectProfileSettings, 500);
    });

    console.log("SendToKindle Custom Context Menu Script Loaded Successfully. Listening for context menus and settings pages...");
})();
