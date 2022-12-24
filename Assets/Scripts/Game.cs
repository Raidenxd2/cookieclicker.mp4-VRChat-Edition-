﻿using System.Collections;
using System.Threading.Tasks;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class Game : UdonSharpBehaviour
{

    //UI
    public Text CookiesText;
    public Text Shop_AutoClickerBTN;
    public Text Shop_DoubleCookieBTN;
    public GameObject NECDialog;

    //Variables
    public double Cookies;
    public double AutoClickers;
    public double AutoClickerPrice;
    public double CPS;
    public double DoubleCookiePrice;
    public double DoubleCookies;
    public double CPC;

    public int TicksPerSecond = 1;
    private float _t;

    //Graphics
    public bool PostProcessing = true;
    public GameObject pp;

    //Other
    UdonBehaviour behaviour;

    void Start()
    {
        behaviour = (UdonBehaviour)GetComponent(typeof(UdonBehaviour));
        _t = 0f;
        ResetData();
    }

    void Update()
    {
        CookiesText.text = "Cookies: " + Cookies;
        Shop_AutoClickerBTN.text = "Buy Autoclicker (" + AutoClickerPrice + " Cookies)";
        Shop_DoubleCookieBTN.text = "Buy Doublecookie (" + DoubleCookiePrice + " Cookies)";
        float dur = 1f / this.TicksPerSecond;
        _t += Time.deltaTime;
        while(_t >= dur)
        {
            _t -= dur;
            this.Tick();
        }

    }

    void Tick()
    {
        Cookies += CPS;
    }

    public void BakeCookie()
    {
        Cookies += CPC;
    }

    public void ResetData()
    {
        Cookies = 0;
        AutoClickers = 0;
        AutoClickerPrice = 25;
        DoubleCookies = 0;
        DoubleCookiePrice = 50;
        CPS = 0;
        CPC = 1;
    }

    public void BuyAutoClicker()
    {
        if (Cookies >= AutoClickerPrice)
        {
            Cookies -= AutoClickerPrice;
            AutoClickers += 1;
            AutoClickerPrice += 25;
            CPS += 1;
        }
        else
        {
            NECDialog.SetActive(true);
        }
    }

    public void BuyDoubleCookie()
    {
        if (Cookies >= DoubleCookiePrice)
        {
            Cookies -= DoubleCookiePrice;
            DoubleCookies += 1;
            DoubleCookiePrice += 50;
            CPC += 1;
        }
        else
        {
            NECDialog.SetActive(true);
        }
    }

    public void PostProcessingToggle()
    {
        PostProcessing = !PostProcessing;
        if (PostProcessing)
        {
            pp.SetActive(true);
        }
        else
        {
            pp.SetActive(false);
        }
    }

}
