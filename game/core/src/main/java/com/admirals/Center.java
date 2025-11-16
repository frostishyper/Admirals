package com.admirals;

import com.badlogic.gdx.Game;

/** {@link com.badlogic.gdx.ApplicationListener} implementation shared by all platforms. */
public class Center extends Game {
    @Override
    public void create() {
        setScreen(new FirstScreen());
    }
}