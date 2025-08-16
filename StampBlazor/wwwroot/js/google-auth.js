let googleAuth = null;

window.initializeGoogleSignIn = async () => {
    return new Promise((resolve) => {
        if (typeof google !== 'undefined') {
            google.accounts.id.initialize({
                client_id: 'YOUR_GOOGLE_CLIENT_ID', // This will need to be replaced with actual client ID
                callback: handleCredentialResponse
            });
            resolve();
        } else {
            // Wait for Google library to load
            setTimeout(() => window.initializeGoogleSignIn().then(resolve), 100);
        }
    });
};

function handleCredentialResponse(response) {
    // This will be called when user signs in with the popup
    // We'll handle this in the signInWithGoogle function instead
}

window.signInWithGoogle = () => {
    return new Promise((resolve, reject) => {
        if (typeof google === 'undefined') {
            reject('Google Sign-In not loaded');
            return;
        }

        google.accounts.id.prompt((notification) => {
            if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
                // Fallback to popup
                google.accounts.oauth2.initTokenClient({
                    client_id: 'YOUR_GOOGLE_CLIENT_ID',
                    scope: 'email profile',
                    callback: (tokenResponse) => {
                        if (tokenResponse.access_token) {
                            // Get user info using the access token
                            fetch(`https://www.googleapis.com/oauth2/v2/userinfo?access_token=${tokenResponse.access_token}`)
                                .then(response => response.json())
                                .then(userInfo => {
                                    resolve({
                                        id: userInfo.id,
                                        email: userInfo.email,
                                        name: userInfo.name,
                                        picture: userInfo.picture
                                    });
                                })
                                .catch(reject);
                        } else {
                            reject('No access token received');
                        }
                    }
                }).requestAccessToken();
            }
        });
    });
};

window.signOutFromGoogle = () => {
    if (typeof google !== 'undefined' && google.accounts) {
        google.accounts.id.disableAutoSelect();
    }
};