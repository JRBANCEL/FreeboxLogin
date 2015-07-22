# FreeboxLogin
Reverse engineering of the Freebox login process

## Why?
The [Freebox V6](https://en.wikipedia.org/wiki/Freebox) exposes an [API](http://dev.freebox.fr/sdk/os/).
An application can talk to this API if it has an app_token. This token can [easily be requested](http://dev.freebox.fr/sdk/os/login/) _but only if_:
* the request comes from the local network (Freebox's own LAN)
* someone has physical access to the Freebox (to press a button)

In the case where one of these conditions can't be met, there is another option: use the web interface. This is what this code does: programmatically act like a user using a browser.

## How?
### Introduction
The Freebox is accessible from internet via HTTP on port 80. It is therefore not secure. Since the login page asks for the password, it can't be sent in cleartext. Authentication is done through a mechanism similar to [SCRAM](https://en.wikipedia.org/wiki/Salted_Challenge_Response_Authentication_Mechanism).

* The client asks for a challenge and the password salt
* The client computes SHA-1(salt + password)
* The client sends HMAC-SHA-1(data=challenge, key=SHA-1(salt + password))

### Step 1 - Fetch the challenge and the password salt
```
GET api/v3/login/
```
```
{"success":true,"result":{"logged_in":false,"challenge":CHALLENGE,"password_salt":PASSWORD_SALT}}
```
**CHALLENGE** is an array of javascript code as a string. An example of value found in **CHALLENGE**:
```
"var _gqphwz = { _jdhuphtz: '_kgxecp' }; _gqphwz._jdhuphtz.charAt(eval(unescape('%76%61%72%20%5F%62%6E%77%74%6E%75%20%3D%20%7B%20%5F%74%76%66%7A%3A%20%30%20%7D%3B%20%5F%62%6E%77%74%6E%75%2E%5F%74%76%66%7A%20%2B%20%34')))"
```

**PASSWORD_SALT** is the salt that was used when the password was created. For example:
```
a6MG696pVHbhFpOEEwtZZUAe3dG6BLTH
```
### Step 2 - Compute the challenge
Each element in **CHALLENGE**, when evaluated, generates a character. The challenge to be used as input of HMAC-SHA-1 is the concatenation of these characters.

### Step 3 - Login
This is done by simulating the form submission:
```
POST /api/v3/login/ HTTP/1.1
Content-Type: application/x-www-form-urlencoded; charset=UTF-8
X-FBX-FREEBOX0S: 1
...
password=HMAC-SHA-1(data=challenge, key=SHA-1(salt + password))
```

The answer sets a cookie with a token:

```
HTTP/1.1 200 OK
Server: nginx
Date: Wed, 22 Jul 2015 04:35:38 GMT
Content-Type: application/json; charset=utf-8
Transfer-Encoding: chunked
Connection: keep-alive
Set-Cookie: FREEBOXOS="TOKEN"; Max-Age=86400; Path=/; HTTPOnly
Content-Encoding: gzip
```

### Step 4 - Profit
With that token, the entire API is accessible by adding it in the Request Headers like a browser would do:

```
GET /api/v3/call/log/ HTTP/1.1
X-Fbx-Freebox0S: 1
Cookie: FREEBOXOS="TOKEN"
```
