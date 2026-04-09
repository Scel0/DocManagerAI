# HOW TO DEPLOY — Step by Step
## You will have a live link in about 5 minutes.

---

## WHAT YOU NEED
- A free GitHub account → https://github.com
- A free Railway account → https://railway.app  (sign in WITH GitHub — one click)

---

## STEP 1 — Push the code to GitHub

1. Go to https://github.com and click **New repository**
2. Name it: `DocManagerAI`
3. Set to **Public** — click **Create repository**
4. GitHub will show you a page with commands. Open a terminal on your computer and run:

```
cd path\to\DocManagerAI        ← the folder you extracted
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/YOURUSERNAME/DocManagerAI.git
git push -u origin main
```

Replace YOURUSERNAME with your actual GitHub username.

---

## STEP 2 — Deploy on Railway

1. Go to https://railway.app
2. Click **Login** → **Login with GitHub**
3. Click **New Project**
4. Click **Deploy from GitHub repo**
5. Select **DocManagerAI** from the list
6. Railway will automatically detect the Dockerfile and start building
7. Wait ~2 minutes for the build to finish (you'll see a progress bar)

---

## STEP 3 — Get your live URL

1. Once deployed, click on your project in Railway
2. Click the **Settings** tab
3. Under **Networking**, click **Generate Domain**
4. Railway gives you a URL like: `https://docmanagerai-production.up.railway.app`

**That's your shareable link. Send it to your seniors.**

---

## LOGIN DETAILS TO SHARE

Send this to your seniors along with the link:

| Username  | Password      | Role                        |
|-----------|---------------|-----------------------------|
| admin     | Admin@1234    | Admin (full access)         |
| reviewer  | Review@1234   | Reviewer (Step 1 approval)  |
| manager   | Manage@1234   | Manager (Step 2 approval)   |
| finance   | Finance@1234  | Finance (Step 3 / final)    |
| viewer    | View@1234     | Viewer (read only)          |

---

## IF YOU GET AN ERROR

**"git is not recognised"** — Download Git from https://git-scm.com/download/win then retry.

**Build fails on Railway** — Click on the failed build, read the logs.
The most common cause is a typo in the Dockerfile — the one provided is correct.

**App loads but can't log in** — The database seeding runs on first startup.
Wait 30 seconds and try again.

---

## FREE TIER LIMITS (Railway)

- $5 free credit per month — enough for testing and demos
- App may sleep after inactivity — first load after sleep takes ~10 seconds
- To keep it always-on, add a credit card on Railway (costs ~$0.50/month at this scale)

