package com.admirals.screens;

import com.admirals.Center;
import com.admirals.managers.ScreenScaler_Manager;
import com.badlogic.gdx.Screen;
import com.badlogic.gdx.graphics.Color;
import com.badlogic.gdx.utils.ScreenUtils;

/**
 * First screen of the application.
 * Renders test output using the manager pipeline.
 */
public class MainMenu_Screen implements Screen {

    private final Center game;
    private final ScreenScaler_Manager scaler;

    /**
     * Constructor.
     * @param game The main game instance (provides managers).
     */
    public MainMenu_Screen(Center game) {
        this.game = game;
        this.scaler = game.screenScalerManager;
    }

    @Override
    public void show() {
        // Unused
    }

    @Override
    public void render(float delta) {
        // Clear Screen
        ScreenUtils.clear(0.1f, 0.1f, 0.1f, 1);

        // Set Scaler
        scaler.getCamera().update();
        game.batch.setProjectionMatrix(scaler.getCamera().combined);

        // Begin Draw batch
        game.batch.begin();

        // Draw Text
        game.fontManager.draw(game.batch, "Reporting Admiral", Color.WHITE, 60f, 100f, 1000f);

        game.batch.end();
    }

    @Override
    public void resize(int width, int height) {
        // Guard against minimized window
        if (width <= 0 || height <= 0) return;
        // The Center class already handled the scaler.resize() call.
    }

    @Override
    public void pause() {}

    @Override
    public void resume() {}

    @Override
    public void hide() {}

    @Override
    public void dispose() {}
}
