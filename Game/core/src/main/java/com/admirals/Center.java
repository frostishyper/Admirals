package com.admirals;

import com.admirals.managers.Asset_Manager;
import com.admirals.managers.Font_Manager;
import com.admirals.screens.MainMenu_Screen;
import com.admirals.utils.Scaling_Util;
import com.badlogic.gdx.Game;


/** {@link com.badlogic.gdx.ApplicationListener} implementation shared by all platforms. */
public class Center extends Game {

    private Scaling_Util scaler;

    @Override
    public void create() {
        // 1. Create scaler
        scaler = new Scaling_Util();

        // 2. Load assets (blocks until finished)
        Asset_Manager.load();

        // 3. Init font manager (needs assets and scaler)
        Font_Manager.init(scaler);

        // 4. Set the first screen
        setScreen(new MainMenu_Screen(this, scaler));
    }

    @Override
    public void dispose() {
        super.dispose();
        // Dispose managers
        Font_Manager.dispose();
        Asset_Manager.dispose();
    }
}
