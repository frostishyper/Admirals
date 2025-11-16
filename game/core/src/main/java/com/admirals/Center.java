package com.admirals;

import com.admirals.managers.Asset_Manager;
import com.admirals.managers.Font_Manager;
import com.admirals.managers.ScreenScaler_Manager;
import com.admirals.screens.MainMenu_Screen;
import com.badlogic.gdx.Game;
import com.badlogic.gdx.graphics.g2d.SpriteBatch;

/**
 * Main application class.
 * Initializes and manages core systems (assets, scaling, fonts).
 */
public class Center extends Game {

    public Asset_Manager assetManager;
    public ScreenScaler_Manager screenScalerManager;
    public Font_Manager fontManager;
    public SpriteBatch batch;

    @Override
    public void create() {
        // Initialize core managers
        assetManager = new Asset_Manager();
        screenScalerManager = new ScreenScaler_Manager();
        batch = new SpriteBatch();

        // Load assets (synchronous)
        // Assumes "Operator" font is mapped in Asset_Manager
        assetManager.loadAssets();

        // Font manager depends on assets and scaler
        fontManager = new Font_Manager(assetManager, screenScalerManager);

        // Pass this game instance (containing managers) to the first screen
        setScreen(new MainMenu_Screen(this));
    }

    @Override
    public void resize(int width, int height) {
        // Pass resize event to the scaler
        screenScalerManager.resize(width, height);
        // Pass to current screen
        super.resize(width, height);
    }

    @Override
    public void render() {
        // Delegates render call to the active screen
        super.render();
    }

    @Override
    public void dispose() {
        // Dispose screens first
        super.dispose();

        // Dispose managers
        fontManager.dispose();
        assetManager.dispose();
        batch.dispose();
    }
}
