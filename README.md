# FreeboxLogin
Reverse engineering of the Freebox login process

## Why?
The Freebox V6 (https://en.wikipedia.org/wiki/Freebox) exposes an API (http://dev.freebox.fr/sdk/os/).
An application can talk to this API if it has an app_token. This token can be easily requested (http://dev.freebox.fr/sdk/os/login/) _but only if_:
* the request comes from the local network (Freebox's own LAN)
* someone has physical access to the Freebox (to press a button)

In the case where one of these conditions can't be met, there is another option: use the web interface. This is what this code does: programmatically act like a user using a browser.
