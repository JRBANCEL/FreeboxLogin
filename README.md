# FreeboxLogin
Reverse engineering of the Freebox login process

## Why?
The [Freebox V6](https://en.wikipedia.org/wiki/Freebox) exposes an [API](http://dev.freebox.fr/sdk/os/).
An application can talk to this API if it has an app_token. This token can [easily be requested](http://dev.freebox.fr/sdk/os/login/) _but only if_:
* the request comes from the local network (Freebox's own LAN)
* someone has physical access to the Freebox (to press a button)

In the case where one of these conditions can't be met, there is another option: use the web interface. This is what this code does: programmatically act like a user using a browser.

## How?
### Step 1 - Fetch the challenge
> GET api/v3/login/

> {"success":true,"result":{"logged_in":false,"challenge":**[CHALLENGE]**,"password_salt":"**[PASSWORD_SALT]**"}}

**[CHALLENGE]** is an array of javascript code as a string. An example of value found in **[CHALLENGE]**:
```
"var _gqphwz = { _jdhuphtz: '_kgxecp' }; _gqphwz._jdhuphtz.charAt(eval(unescape('%76%61%72%20%5F%62%6E%77%74%6E%75%20%3D%20%7B%20%5F%74%76%66%7A%3A%20%30%20%7D%3B%20%5F%62%6E%77%74%6E%75%2E%5F%74%76%66%7A%20%2B%20%34')))"
```
