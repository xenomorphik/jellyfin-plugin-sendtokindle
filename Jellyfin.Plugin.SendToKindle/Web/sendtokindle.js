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

    const isShelfmarkEnabled = async () => {
        try {
            let res = await fetch(ApiClient.getUrl('SendToKindle/ShelfmarkEnabled'), {
                headers: { 'Authorization': 'MediaBrowser Token="' + ApiClient.accessToken() + '"' }
            });
            if (res.ok) {
                return await res.json();
            }
        } catch(e) { }
        return false;
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
                
                <div class="inputContainer" style="margin-top: 2em;">
                    <label class="inputLabel" for="fileUploadKindle">Direct Upload to Kindle</label>
                    <input type="file" id="fileUploadKindle" accept=".epub,.mobi,.azw3,.pdf,.cbz,.cbr,.prc,.pdb" style="width: 100%; max-width: 400px; padding: .5em; border: 1px solid #555; border-radius: 4px; background: rgba(0,0,0,0.2); color: inherit;" />
                    <div class="fieldDescription">Choose a book from your local device to send directly. (EPUB, MOBI, AZW3, PDF, CBZ, CBR, PRC, PDB)</div>
                </div>
                <button is="emby-button" type="button" class="raised block emby-button" id="btnUploadToKindle" style="margin-top: 1em; padding: 0.5em 1em; background-color: #00a4dc; color: white; border: none; border-radius: 4px; cursor: pointer;">
                    <span>Upload & Send</span>
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

            document.getElementById('btnUploadToKindle').addEventListener('click', async (e) => {
                e.preventDefault();
                const fileInput = document.getElementById('fileUploadKindle');
                if (fileInput.files.length === 0) {
                    require(['toast'], function (toast) { toast("Please select a file to upload."); });
                    return;
                }
                const file = fileInput.files[0];
                
                const formData = new FormData();
                formData.append('file', file);
                
                try {
                    Dashboard.showLoadingMsg();
                    let res = await fetch(ApiClient.getUrl('SendToKindle/Upload'), {
                        method: 'POST',
                        headers: {
                            'Authorization': 'MediaBrowser Token="' + ApiClient.accessToken() + '"'
                        },
                        body: formData
                    });
                    Dashboard.hideLoadingMsg();
                    
                    if (res.status === 412) {
                        require(['toast'], function (toast) { toast("Please set and save your Target Kindle Email first."); });
                        return;
                    }
                    if (!res.ok) {
                        let errorText = "Upload failed";
                        try { 
                            errorText = await res.text(); 
                            if (!errorText) errorText = "Upload failed";
                        } catch(e) {}
                        throw new Error(errorText);
                    }
                    
                    require(['toast'], function (toast) {
                        toast("Delivery Queued! Your uploaded book will be sent to your Kindle shortly.");
                    });
                    fileInput.value = "";
                } catch (err) {
                    Dashboard.hideLoadingMsg();
                    require(['toast'], function (toast) {
                        toast(err.message === "Upload failed" ? "Failed to upload to Kindle. See logs." : err.message);
                    });
                }
            });

            isShelfmarkEnabled().then(enabled => {
                if (enabled) {
                    let smSection = document.createElement('div');
                    smSection.style.cssText = "margin-top: 2em; padding-top: 1em; border-top: 1px solid rgba(255,255,255,0.1);";
                    smSection.innerHTML = `
                        <h2 class="detailSectionTitle">Search Books (Shelfmark)</h2>
                        <div class="inputContainer">
                            <label class="inputLabel" for="shelfmarkQuery">Search Title or Author</label>
                            <div style="display:flex;gap:10px;">
                                <input type="text" id="shelfmarkQuery" placeholder="Book Title or Author" style="flex: 1; padding: .5em; border: 1px solid #555; border-radius: 4px; background: rgba(0,0,0,0.2); color: inherit;" />
                                <button is="emby-button" type="button" class="raised emby-button" id="btnSearchShelfmark" style="padding: 0.5em 1em; background-color: #00a4dc; color: white; border: none; border-radius: 4px; cursor: pointer;">
                                    <span>Search</span>
                                </button>
                            </div>
                        </div>
                        <div id="shelfmarkResults" style="margin-top:1em; display:flex; flex-direction:column; gap:10px;"></div>
                    `;
                    section.appendChild(smSection);

                    document.getElementById('btnSearchShelfmark').addEventListener('click', async (e) => {
                        e.preventDefault();
                        let q = document.getElementById('shelfmarkQuery').value;
                        if (!q) return;

                        Dashboard.showLoadingMsg();
                        try {
                            let res = await fetch(ApiClient.getUrl('SendToKindle/ShelfmarkProxy/api/metadata/search?query=' + encodeURIComponent(q)), {
                                headers: { 'Authorization': 'MediaBrowser Token="' + ApiClient.accessToken() + '"' }
                            });
                            let resData = await res.json();
                            let data = resData.books || resData || [];
                            let resultsDiv = document.getElementById('shelfmarkResults');
                            resultsDiv.innerHTML = "";

                            if (!data || data.length === 0) {
                                resultsDiv.innerHTML = "<div>No results found.</div>";
                            } else {
                                data.slice(0, 10).forEach(book => {
                                    let div = document.createElement('div');
                                    div.style.cssText = "display:flex; justify-content:space-between; align-items:center; background:rgba(0,0,0,0.2); padding:10px; border-radius:4px;";
                                    div.innerHTML = `
                                        <div style="flex:1;">
                                            <strong>${book.title}</strong><br>
                                            <small>${book.authors ? book.authors.join(', ') : 'Unknown Author'}</small>
                                        </div>
                                        <button is="emby-button" class="raised emby-button btnDownloadShelfmark" data-book='${JSON.stringify(book).replace(/'/g, "&apos;")}' style="padding: 0.5em; background-color: #00a4dc; color: white; border: none; border-radius: 4px; cursor: pointer;">
                                            Request / Download
                                        </button>
                                    `;
                                    resultsDiv.appendChild(div);
                                });

                                resultsDiv.querySelectorAll('.btnDownloadShelfmark').forEach(btn => {
                                    btn.addEventListener('click', async (ev) => {
                                        ev.preventDefault();
                                        let bookData = JSON.parse(btn.getAttribute('data-book').replace(/&apos;/g, "'"));
                                        Dashboard.showLoadingMsg();
                                        try {
                                            let reqRes = await fetch(ApiClient.getUrl('SendToKindle/ShelfmarkProxy/api/requests'), {
                                                method: 'POST',
                                                headers: {
                                                    'Authorization': 'MediaBrowser Token="' + ApiClient.accessToken() + '"',
                                                    'Content-Type': 'application/json'
                                                },
                                                body: JSON.stringify({
                                                    context: { source: "jellyfin", request_level: "book" },
                                                    book_data: {
                                                        provider: bookData.provider || "hardcover",
                                                        book_id: bookData.id || bookData.book_id || bookData.provider_id,
                                                        title: bookData.title
                                                    }
                                                })
                                            });
                                            if (reqRes.ok) {
                                                require(['toast'], function (toast) { toast("Request sent to Shelfmark! The book will be downloaded and processed shortly."); });
                                            } else {
                                                let errText = await reqRes.text();
                                                require(['toast'], function (toast) { toast("Failed to send request: " + errText); });
                                            }
                                        } catch (e) {
                                            require(['toast'], function (toast) { toast("Error: " + e.message); });
                                        }
                                        Dashboard.hideLoadingMsg();
                                    });
                                });
                            }
                        } catch (e) {
                            require(['toast'], function (toast) { toast("Search failed: " + e.message); });
                        }
                        Dashboard.hideLoadingMsg();
                    });
                }
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
